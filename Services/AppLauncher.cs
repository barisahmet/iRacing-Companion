using System.Diagnostics;
using System.IO;
using IRacingSmartPlug.Models;

namespace IRacingSmartPlug.Services;

/// <summary>Launches managed companion apps.</summary>
public sealed class AppLauncher
{
    private readonly LogService _log;

    public AppLauncher(LogService log) => _log = log;

    public bool IsRunning(ManagedApp app, HashSet<string> running) =>
        ProcessMonitor.IsRunning(running, app.EffectiveProcessName);

    /// <summary>Launch the app unless it is already running. Returns true if a launch happened.</summary>
    public bool Launch(ManagedApp app, HashSet<string> running, bool force = false)
    {
        if (!force && !app.Enabled)
            return false;
        if (IsRunning(app, running))
        {
            _log.Info($"App '{app.Name}' already running");
            return false;
        }
        if (string.IsNullOrWhiteSpace(app.Path) || !File.Exists(app.Path))
        {
            _log.Warn($"App not found: {(string.IsNullOrWhiteSpace(app.Path) ? "(no path)" : app.Path)}");
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = app.Path,
                Arguments = app.Arguments ?? "",
                WorkingDirectory = Path.GetDirectoryName(app.Path) ?? "",
                UseShellExecute = true,
                WindowStyle = app.StartMinimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal
            };
            Process.Start(psi);
            _log.Info($"Launched '{app.Name}'{(app.StartMinimized ? " (minimized)" : "")}");
            return true;
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to launch '{app.Name}': {ex.Message}");
            return false;
        }
    }
}
