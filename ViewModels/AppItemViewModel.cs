using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IRacingSmartPlug.Models;

namespace IRacingSmartPlug.ViewModels;

/// <summary>
/// A row in the Apps list. Doubles as its own inline editor: the card expands
/// into an edit form (no popup) and collapses back on save/cancel.
/// </summary>
public sealed partial class AppItemViewModel : ObservableObject
{
    private readonly AppsViewModel _parent;

    public ManagedApp Model { get; }
    public bool IsNew { get; private set; }

    public record TriggerOption(LaunchTrigger Value, string Display);

    public IReadOnlyList<TriggerOption> Triggers { get; } =
        Enum.GetValues<LaunchTrigger>().Select(v => new TriggerOption(v, v.ToDisplay())).ToList();

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isEditing;

    // header on/off toggle (single source of truth for Enabled)
    [ObservableProperty] private bool _enabled;

    // edit-form fields
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editPath = "";
    [ObservableProperty] private string _editArguments = "";
    [ObservableProperty] private string _editProcessName = "";
    [ObservableProperty] private LaunchTrigger _editTrigger = LaunchTrigger.IRacingUi;
    [ObservableProperty] private bool _editStartMinimized;
    [ObservableProperty] private string _editError = "";

    public AppItemViewModel(ManagedApp model, AppsViewModel parent,
                            bool isNew = false, bool startEditing = false)
    {
        Model = model;
        _parent = parent;
        IsNew = isNew;
        _enabled = model.Enabled;
        if (startEditing) BeginEdit();
    }

    // ---- header display ---- //
    public string DisplayName => string.IsNullOrWhiteSpace(Model.Name) ? "New app" : Model.Name;
    public string PathText => string.IsNullOrWhiteSpace(Model.Path) ? "No executable selected yet" : Model.Path;

    public string TagsText
    {
        get
        {
            var tags = new List<string> { Model.Trigger.ToDisplay() };
            if (Model.StartMinimized) tags.Add("minimized");
            return string.Join("   ·   ", tags);
        }
    }

    partial void OnEnabledChanged(bool value)
    {
        Model.Enabled = value;
        _parent.Persist();
    }

    // ---- editing ---- //
    [RelayCommand]
    private void ToggleEdit()
    {
        if (IsEditing) Cancel();
        else BeginEdit();
    }

    [RelayCommand]
    private void Edit() => BeginEdit();

    private void BeginEdit()
    {
        EditName = Model.Name;
        EditPath = Model.Path;
        EditArguments = Model.Arguments;
        EditProcessName = Model.ProcessName;
        EditTrigger = Model.Trigger;
        EditStartMinimized = Model.StartMinimized;
        EditError = "";
        IsEditing = true;
    }

    [RelayCommand]
    private void Browse()
    {
        var picked = _parent.PickExecutable(EditPath);
        if (picked is null) return;
        EditPath = picked;
        if (string.IsNullOrWhiteSpace(EditName))
            EditName = Path.GetFileNameWithoutExtension(picked);
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(EditName)) { EditError = "Name is required."; return; }
        if (string.IsNullOrWhiteSpace(EditPath)) { EditError = "Choose an executable."; return; }

        Model.Name = EditName.Trim();
        Model.Path = EditPath.Trim();
        Model.Arguments = EditArguments.Trim();
        Model.ProcessName = EditProcessName.Trim();
        Model.Trigger = EditTrigger;
        Model.StartMinimized = EditStartMinimized;
        // Enabled is controlled by the on/off switch, not the editor.

        IsNew = false;
        IsEditing = false;
        RefreshDisplay();
        _parent.Persist();
        _parent.Note = $"Saved {Model.Name}";
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        if (IsNew) _parent.RemoveItem(this);
    }

    private void RefreshDisplay()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(PathText));
        OnPropertyChanged(nameof(TagsText));
    }

    [RelayCommand] private void Launch() => _parent.LaunchApp(this);
    [RelayCommand] private void Remove() => _parent.RemoveApp(this);
}
