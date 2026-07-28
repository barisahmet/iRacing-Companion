using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IRacingSmartPlug.Models;
using IRacingSmartPlug.Services;

namespace IRacingSmartPlug.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly PlugOrchestrator _orchestrator;
    private readonly StartupService _startup;

    public record PlugTriggerOption(PlugTrigger Value, string Display);

    public IReadOnlyList<PlugTriggerOption> PlugTriggers { get; } = new[]
    {
        new PlugTriggerOption(PlugTrigger.Either, "iRacing UI or Simulator"),
        new PlugTriggerOption(PlugTrigger.IRacingUi, "iRacing UI only"),
        new PlugTriggerOption(PlugTrigger.Simulator, "Simulator only")
    };

    [ObservableProperty] private string _baseUrl = "";
    [ObservableProperty] private string _token = "";
    [ObservableProperty] private string _entityId = "";
    [ObservableProperty] private string _uiProcessName = "";
    [ObservableProperty] private string _simProcessName = "";
    [ObservableProperty] private int _pollIntervalSeconds;
    [ObservableProperty] private int _offDelaySeconds;
    [ObservableProperty] private int _requestTimeoutSeconds;
    [ObservableProperty] private PlugTrigger _plugTrigger;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private string _note = "";

    public SettingsViewModel(ConfigService config, PlugOrchestrator orchestrator, StartupService startup)
    {
        _config = config;
        _orchestrator = orchestrator;
        _startup = startup;
        LoadFromConfig();
    }

    public void LoadFromConfig()
    {
        var c = _config.Current;
        BaseUrl = c.HomeAssistant.BaseUrl;
        Token = c.HomeAssistant.Token;
        EntityId = c.HomeAssistant.EntityId;
        UiProcessName = c.Behavior.UiProcessName;
        SimProcessName = c.Behavior.SimProcessName;
        PollIntervalSeconds = c.Behavior.PollIntervalSeconds;
        OffDelaySeconds = c.Behavior.OffDelaySeconds;
        RequestTimeoutSeconds = c.Behavior.RequestTimeoutSeconds;
        PlugTrigger = c.Behavior.PlugTrigger;
        StartWithWindows = _startup.IsEnabled();
    }

    [RelayCommand]
    private void Save()
    {
        var c = _config.Current;
        c.HomeAssistant.BaseUrl = BaseUrl.Trim();
        c.HomeAssistant.Token = Token.Trim();
        c.HomeAssistant.EntityId = EntityId.Trim();
        c.Behavior.UiProcessName = UiProcessName.Trim();
        c.Behavior.SimProcessName = SimProcessName.Trim();
        c.Behavior.PollIntervalSeconds = Math.Max(1, PollIntervalSeconds);
        c.Behavior.OffDelaySeconds = Math.Max(0, OffDelaySeconds);
        c.Behavior.RequestTimeoutSeconds = Math.Max(1, RequestTimeoutSeconds);
        c.Behavior.PlugTrigger = PlugTrigger;
        _config.Save();

        try { _startup.SetEnabled(StartWithWindows); }
        catch (Exception ex) { Note = $"Startup toggle failed: {ex.Message}"; }

        _orchestrator.RequestRefresh();
        Note = "Saved";
    }

    [RelayCommand]
    private void Reload()
    {
        _config.Load();
        LoadFromConfig();
        Note = "Reloaded";
    }

    [RelayCommand]
    private void TestConnection()
    {
        Save();
        _orchestrator.RequestRefresh();
        Note = "Testing connection — see Dashboard / Logs";
    }
}
