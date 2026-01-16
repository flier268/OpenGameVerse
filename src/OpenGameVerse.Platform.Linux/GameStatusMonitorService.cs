using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenGameVerse.Core.Services;

namespace OpenGameVerse.Platform.Linux;

public sealed class GameStatusMonitorService : GameStatusMonitorServiceBase
{
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

        var seen = new HashSet<int>();
        foreach (var process in Process.GetProcesses())
        {
            string? path;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch
            {
                continue;
            }

            if (path is null)
            {
                continue;
            }

            seen.Add(process.Id);
            var cmdlinePath = Path.Combine(procRoot, process.Id.ToString(), "cmdline");
            list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName, cmdlinePath));
        }

        PruneCmdlineCache(seen);
        return new ReadOnlyCollection<ProcessInfo>(list);
    }
}
