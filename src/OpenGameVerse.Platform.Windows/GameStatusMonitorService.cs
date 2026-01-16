using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenGameVerse.Core.Services;

namespace OpenGameVerse.Platform.Windows;

public sealed class GameStatusMonitorService : GameStatusMonitorServiceBase
{
    private const int ProcessCacheTtlTicks = 64;
    private static readonly Lock ProcessCacheLock = new();
    private static readonly Dictionary<(int Id, string Name), CachedProcessInfo> ProcessCache = new();

    public GameStatusMonitorService(TimeSpan? interval = null)
        : base(interval)
    {
    }

    protected override StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;

    protected override ReadOnlyCollection<ProcessInfo> GetProcessSnapshot()
    {
        var list = new List<ProcessInfo>();
        DecayProcessCache();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var (path, commandLine) = GetProcessData(process);
                list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName, commandLine));
            }
            catch
            {
                // Ignore process access failures
            }
        }

        return new ReadOnlyCollection<ProcessInfo>(list);
    }

    private static (string? Path, string? CommandLine) GetProcessData(Process process)
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
            path = null;
        }

        lock (ProcessCacheLock)
        {
            // 將重取資料這件事分散在不同tick，降低資源佔用
            ProcessCache[key] = new CachedProcessInfo(path, null, Random.Shared.Next(1, ProcessCacheTtlTicks));
        }

        return (path, null);
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

    private sealed record CachedProcessInfo(string? Path, string? CommandLine, int RemainingTicks);
}