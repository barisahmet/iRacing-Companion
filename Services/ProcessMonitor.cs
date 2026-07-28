using System.Diagnostics;

namespace IRacingSmartPlug.Services;

/// <summary>
/// Snapshots running process names so the orchestrator and UI can cheaply test
/// whether iRacing / the simulator / a companion app is running.
/// </summary>
public sealed class ProcessMonitor
{
    /// <summary>Refresh and return the set of running process names (lowercased, no extension).</summary>
    public HashSet<string> Snapshot()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                names.Add(p.ProcessName); // e.g. "iRacingUI" (no .exe)
            }
            catch
            {
                // Process may have exited between enumeration and read.
            }
            finally
            {
                p.Dispose();
            }
        }
        return names;
    }

    /// <summary>True if a process matching the given name (with or without .exe) is in the set.</summary>
    public static bool IsRunning(HashSet<string> running, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        var bare = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        return running.Contains(bare);
    }
}
