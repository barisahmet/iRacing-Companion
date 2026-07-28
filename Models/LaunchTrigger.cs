namespace IRacingSmartPlug.Models;

/// <summary>
/// When a managed app should be launched.
/// </summary>
public enum LaunchTrigger
{
    /// <summary>Launch when the iRacing UI / menu app opens (iRacingUI.exe).</summary>
    IRacingUi = 0,

    /// <summary>Launch when the on-track simulator opens (iRacingSim64DX11.exe).</summary>
    Simulator = 1,

    /// <summary>Launch as soon as either the UI or the simulator is running.</summary>
    Either = 2
}

public static class LaunchTriggerExtensions
{
    public static string ToDisplay(this LaunchTrigger t) => t switch
    {
        LaunchTrigger.IRacingUi => "iRacing UI opens",
        LaunchTrigger.Simulator => "Simulator (on track) opens",
        LaunchTrigger.Either => "Either UI or Sim",
        _ => t.ToString()
    };
}
