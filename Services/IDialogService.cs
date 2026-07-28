namespace IRacingSmartPlug.Services;

public interface IDialogService
{
    /// <summary>Yes/No confirmation. Returns true if the user confirmed.</summary>
    bool Confirm(string title, string message);

    /// <summary>Open a file picker for an executable. Returns the path, or null if cancelled.</summary>
    string? PickExecutable(string? currentPath);
}
