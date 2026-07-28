using System.IO;
using System.Text.Json.Serialization;

namespace IRacingSmartPlug.Models;

/// <summary>
/// A companion application that can be launched automatically with iRacing.
/// </summary>
public sealed class ManagedApp
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    /// <summary>Full path to the executable.</summary>
    public string Path { get; set; } = "";

    /// <summary>Optional command-line arguments passed on launch.</summary>
    public string Arguments { get; set; } = "";

    /// <summary>
    /// Process name used to detect if it is already running. Falls back to the
    /// executable file name when blank.
    /// </summary>
    public string ProcessName { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public bool StartMinimized { get; set; }

    public LaunchTrigger Trigger { get; set; } = LaunchTrigger.IRacingUi;

    /// <summary>Effective process name (explicit, or derived from the path).</summary>
    [JsonIgnore]
    public string EffectiveProcessName =>
        !string.IsNullOrWhiteSpace(ProcessName)
            ? ProcessName
            : (string.IsNullOrWhiteSpace(Path) ? "" : System.IO.Path.GetFileName(Path));

    public ManagedApp Clone() => new()
    {
        Id = Id,
        Name = Name,
        Path = Path,
        Arguments = Arguments,
        ProcessName = ProcessName,
        Enabled = Enabled,
        StartMinimized = StartMinimized,
        Trigger = Trigger
    };
}
