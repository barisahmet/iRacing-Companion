using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IRacingSmartPlug.Models;
using IRacingSmartPlug.Services;

namespace IRacingSmartPlug.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly PlugOrchestrator _orchestrator;

    private bool _updatingFromState;
    private int _switchPending;

    [ObservableProperty] private bool _plugOn;
    [ObservableProperty] private string _plugStateText = "Unknown";
    [ObservableProperty] private int _plugCode = -1;   // 1 on, 0 off, -1 unknown

    [ObservableProperty] private bool _iRacingOpen;
    [ObservableProperty] private string _iRacingStatusText = "iRacing closed";

    [ObservableProperty] private bool _uiActive;
    [ObservableProperty] private string _uiStateText = "Not running";

    [ObservableProperty] private bool _simActive;
    [ObservableProperty] private string _simStateText = "Not running";

    [ObservableProperty] private string _haStatusText = "starting";
    [ObservableProperty] private int _haState; // 0 unknown, 1 ok, 2 warn, 3 error

    [ObservableProperty] private string _appsSummary = "";

    public DashboardViewModel(ConfigService config, PlugOrchestrator orchestrator)
    {
        _config = config;
        _orchestrator = orchestrator;
    }

    partial void OnPlugOnChanged(bool value)
    {
        if (_updatingFromState) return;
        _switchPending = 4; // ignore state echoes for a few ticks
        PlugStateText = value ? "On" : "Off";
        PlugCode = value ? 1 : 0;
        _orchestrator.RequestPlug(value);
    }

    private static string Prettify(string status) => status switch
    {
        "connected" => "Connected",
        "unreachable" => "Unreachable",
        "starting" => "Starting…",
        _ when status.Contains("unauthorized") => "Unauthorized",
        _ => status
    };

    public void RefreshFrom(AppState.Snapshot s)
    {
        UiActive = s.UiRunning;
        UiStateText = s.UiRunning ? "Running" : "Not running";
        SimActive = s.SimRunning;
        SimStateText = s.SimRunning ? "On track" : "Not running";

        IRacingOpen = s.UiRunning || s.SimRunning;
        IRacingStatusText = IRacingOpen ? "iRacing open" : "iRacing closed";

        HaStatusText = Prettify(s.HaStatus);
        HaState = s.HaStatus switch
        {
            "connected" => 1,
            "unreachable" => 2,
            _ when s.HaStatus.Contains("unauthorized") => 3,
            _ => 0
        };

        if (_switchPending > 0)
        {
            _switchPending--;
        }
        else
        {
            _updatingFromState = true;
            PlugOn = s.Plug is true;
            _updatingFromState = false;
            PlugStateText = s.Plug is true ? "On" : s.Plug is false ? "Off" : "Unknown";
            PlugCode = s.Plug is true ? 1 : s.Plug is false ? 0 : -1;
        }

        var apps = _config.Current.Apps;
        var auto = apps.Count(a => a.Enabled);
        AppsSummary = apps.Count == 0
            ? "No apps configured"
            : $"{apps.Count} configured · {auto} enabled";
    }

    [RelayCommand] private void RefreshState() => _orchestrator.RequestRefresh();
}
