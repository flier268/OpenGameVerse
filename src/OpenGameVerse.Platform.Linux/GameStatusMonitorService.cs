using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenGameVerse.Core.Services;

namespace OpenGameVerse.Platform.Linux;

public sealed class GameStatusMonitorService : GameStatusMonitorServiceBase
{
    private const int ProcessCacheTtlTicks = 64;
    private static readonly Lock ProcessCacheLock = new();
    private static readonly Dictionary<(int Id, string Name), CachedProcessInfo> ProcessCache = new();

    public GameStatusMonitorService(TimeSpan? interval = null)
        : base(interval)
    {
    }

    protected override StringComparison PathComparison => StringComparison.Ordinal;

    protected override bool IncludeWinePath => true;

    protected override bool IsInstallPathTooBroad(string installPath)
    {
        var normalized = installPath.TrimEnd(Path.DirectorySeparatorChar);
        if (string.IsNullOrEmpty(normalized))
        {
            return true;
        }

        return string.Equals(normalized, "/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/local/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/snap/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/share", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/share/applications", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/local/share", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/local/share/applications", StringComparison.Ordinal)
               || string.Equals(normalized, "/lib", StringComparison.Ordinal)
               || string.Equals(normalized, "/lib64", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/lib", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/lib64", StringComparison.Ordinal);
    }

    protected override ReadOnlyCollection<ProcessInfo> GetProcessSnapshot()
    {
        var list = new List<ProcessInfo>();
        const string procRoot = "/proc";

        if (!Directory.Exists(procRoot))
        {
            return new ReadOnlyCollection<ProcessInfo>(list);
        }

        DecayProcessCache();
        foreach (var process in Process.GetProcesses())
        {
            var (path, commandLine) = GetProcessData(process, procRoot);
            if (path is null)
            {
                continue;
            }

            list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName, commandLine));
        }

        return new ReadOnlyCollection<ProcessInfo>(list);
    }

    private static (string? Path, string? CommandLine) GetProcessData(Process process, string procRoot)
    {
        var name = process.ProcessName ?? string.Empty;
        var key = (process.Id, name);

        lock (ProcessCacheLock)
        {
            if (ProcessCache.TryGetValue(key, out var cached))
            {
                return (cached.Path, cached.CommandLine);
            }
        }

        string? path;
        try
        {
            path = process.MainModule?.FileName;
        }
        catch
        {
            return (null, null);
        }

        var commandLine = TryReadCmdline(Path.Combine(procRoot, process.Id.ToString(), "cmdline"));

        lock (ProcessCacheLock)
        {
            if (path is null && commandLine is null)
            {
                // 這個久久更新一次就好
                ProcessCache[key] = new CachedProcessInfo(path, commandLine, 999);
            }
            else
            {
                // 將重取資料這件事分散在不同tick，降低資源佔用
                ProcessCache[key] =
                    new CachedProcessInfo(path, commandLine, Random.Shared.Next(1, ProcessCacheTtlTicks));
            }
        }

        return (path, commandLine);
    }

    private static void DecayProcessCache()
    {
        lock (ProcessCacheLock)
        {
            if (ProcessCache.Count == 0)
            {
                return;
            }

            var toRemove = new List<(int Id, string Name)>();
            var toUpdate = new List<((int Id, string Name) Key, CachedProcessInfo Value)>();
            foreach (var (key, value) in ProcessCache)
            {
                var remaining = value.RemainingTicks - 1;
                if (remaining <= 0)
                {
                    toRemove.Add(key);
                }
                else
                {
                    toUpdate.Add((key, value with { RemainingTicks = remaining }));
                }
            }

            foreach (var key in toRemove)
            {
                ProcessCache.Remove(key);
            }

            foreach (var (key, value) in toUpdate)
            {
                ProcessCache[key] = value;
            }
        }
    }

    private static string? TryReadCmdline(string path)
    {
        try
        {
            var data = File.ReadAllText(path);
            return data.TrimEnd('\0');
        }
        catch
        {
            return null;
        }
    }

    private sealed record CachedProcessInfo(string? Path, string? CommandLine, int RemainingTicks);
}