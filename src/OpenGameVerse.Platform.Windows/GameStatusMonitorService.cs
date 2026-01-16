using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenGameVerse.Core.Services;

namespace OpenGameVerse.Platform.Windows;

public sealed class GameStatusMonitorService : GameStatusMonitorServiceBase
{
    public GameStatusMonitorService(TimeSpan? interval = null)
        : base(interval)
    {
    }

    protected override StringComparison PathComparison => StringComparison.OrdinalIgnoreCase;

    protected override ReadOnlyCollection<ProcessInfo> GetProcessSnapshot()
    {
        var list = new List<ProcessInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                string? path = null;
                try
                {
                    path = process.MainModule?.FileName;
                }
                catch
                {
                    path = null;
                }

                list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName));
            }
            catch
            {
                // Ignore process access failures
            }
        }

        return new ReadOnlyCollection<ProcessInfo>(list);
    }
}
