using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IRacingSmartPlug.Services;

namespace IRacingSmartPlug.ViewModels;

/// <summary>
/// Owns the child page view-models, handles navigation, and drives a UI-thread
/// timer that pulls live state from <see cref="AppState"/> into the view-models.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly DispatcherTimer _timer;

    public DashboardViewModel Dashboard { get; }
    public AppsViewModel Apps { get; }
    public SettingsViewModel Settings { get; }
    public LogsViewModel Logs { get; }

    [ObservableProperty] private ObservableObject? _currentPage;
    [ObservableProperty] private string _selectedTag = "dashboard";

    public MainViewModel(AppState state, DashboardViewModel dashboard, AppsViewModel apps,
                         SettingsViewModel settings, LogsViewModel logs)
    {
        _state = state;
        Dashboard = dashboard;
        Apps = apps;
        Settings = settings;
        Logs = logs;
        _currentPage = dashboard;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Pump();
        _timer.Start();
    }

    partial void OnSelectedTagChanged(string value)
    {
        CurrentPage = value switch
        {
            "apps" => Apps,
            "settings" => Settings,
            "logs" => Logs,
            _ => Dashboard
        };
    }

    [RelayCommand]
    private void Navigate(string tag) => SelectedTag = tag;

    private void Pump()
    {
        var snapshot = _state.Read();
        Dashboard.RefreshFrom(snapshot);
        Apps.RefreshRunning(snapshot.Running);
        Logs.Refresh();
    }
}
