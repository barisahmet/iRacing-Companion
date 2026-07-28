using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IRacingSmartPlug.Models;
using IRacingSmartPlug.Services;

namespace IRacingSmartPlug.ViewModels;

public sealed partial class AppsViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly PlugOrchestrator _orchestrator;
    private readonly IDialogService _dialogs;
    private readonly LogService _log;

    public ObservableCollection<AppItemViewModel> Apps { get; } = new();

    [ObservableProperty] private string _note = "";
    [ObservableProperty] private bool _isEmpty;

    public AppsViewModel(ConfigService config, PlugOrchestrator orchestrator,
                         IDialogService dialogs, LogService log)
    {
        _config = config;
        _orchestrator = orchestrator;
        _dialogs = dialogs;
        _log = log;
        Reload();
    }

    public void Reload()
    {
        Apps.Clear();
        foreach (var m in _config.Current.Apps)
            Apps.Add(new AppItemViewModel(m, this));
        UpdateEmpty();
    }

    /// <summary>Persist every non-draft item's model back to config.</summary>
    public void Persist()
    {
        _config.Current.Apps = Apps.Where(a => !a.IsNew).Select(a => a.Model).ToList();
        _config.Save();
    }

    [RelayCommand]
    private void AddApp()
    {
        // Collapse any other open editor for a clean single-focus experience.
        foreach (var a in Apps.Where(a => a.IsEditing).ToList())
        {
            if (a.IsNew) Apps.Remove(a);
            else a.IsEditing = false;
        }
        var item = new AppItemViewModel(new ManagedApp(), this, isNew: true, startEditing: true);
        Apps.Insert(0, item);
        UpdateEmpty();
    }

    public void RemoveItem(AppItemViewModel item)
    {
        Apps.Remove(item);
        UpdateEmpty();
    }

    public void RemoveApp(AppItemViewModel item)
    {
        if (!_dialogs.Confirm("Remove app", $"Remove '{item.DisplayName}' from the list?"))
            return;
        Apps.Remove(item);
        Persist();
        UpdateEmpty();
        Note = $"Removed {item.DisplayName}";
    }

    public void LaunchApp(AppItemViewModel item)
    {
        _orchestrator.RequestLaunch(item.Model.Id);
        Note = $"Launching {item.DisplayName}…";
    }

    public string? PickExecutable(string? current) => _dialogs.PickExecutable(current);

    /// <summary>Update the green "running" dots from a process snapshot.</summary>
    public void RefreshRunning(HashSet<string> running)
    {
        foreach (var item in Apps)
            item.IsRunning = ProcessMonitor.IsRunning(running, item.Model.EffectiveProcessName);
    }

    private void UpdateEmpty() => IsEmpty = Apps.Count == 0;
}
