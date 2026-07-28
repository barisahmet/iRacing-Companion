using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace IRacingSmartPlug.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message)
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow;
        var result = owner is not null
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? PickExecutable(string? currentPath)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select application",
            Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*"
        };
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            var dir = Path.GetDirectoryName(currentPath);
            if (Directory.Exists(dir)) dlg.InitialDirectory = dir;
        }
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
