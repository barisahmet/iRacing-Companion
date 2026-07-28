namespace IRacingSmartPlug.Models;

/// <summary>Which sessions should power the smart plug on.</summary>
public enum PlugTrigger
{
    IRacingUi = 0,
    Simulator = 1,
    Either = 2
}

public sealed class HomeAssistantSettings
{
    public string BaseUrl { get; set; } = "http://homeassistant.local:8123";
    public string Token { get; set; } = "";
    public string EntityId { get; set; } = "switch.racing_plug";
}

public sealed class BehaviorSettings
{
    public string UiProcessName { get; set; } = "iRacingUI.exe";
    public string SimProcessName { get; set; } = "iRacingSim64DX11.exe";
    public int PollIntervalSeconds { get; set; } = 5;
    public int OffDelaySeconds { get; set; } = 150;
    public int RequestTimeoutSeconds { get; set; } = 10;
    public PlugTrigger PlugTrigger { get; set; } = PlugTrigger.Either;
}

public sealed class WindowBounds
{
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Maximized { get; set; }
}

/// <summary>Root configuration object, serialized to config.json.</summary>
public sealed class AppConfig
{
    public HomeAssistantSettings HomeAssistant { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public List<ManagedApp> Apps { get; set; } = new();
    public bool StartWithWindows { get; set; }
    public bool StartMinimizedToTray { get; set; } = true;
    public WindowBounds Window { get; set; } = new();
}
