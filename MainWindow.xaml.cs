using System.ComponentModel;
using System.Windows;
using IRacingSmartPlug.Services;
using Wpf.Ui.Controls;

namespace IRacingSmartPlug;

public partial class MainWindow : FluentWindow
{
    private readonly ConfigService _config;

    public MainWindow(ConfigService config)
    {
        _config = config;
        InitializeComponent();
        RestoreWindowBounds();
    }

    private void RestoreWindowBounds()
    {
        var w = _config.Current.Window;

        if (w.Width is double width && width >= MinWidth &&
            w.Height is double height && height >= MinHeight)
        {
            Width = width;
            Height = height;
        }

        if (w.Left is double left && w.Top is double top && IsOnScreen(left, top))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        if (w.Maximized)
            WindowState = WindowState.Maximized;
    }

    private static bool IsOnScreen(double left, double top)
    {
        double l = SystemParameters.VirtualScreenLeft;
        double t = SystemParameters.VirtualScreenTop;
        double r = l + SystemParameters.VirtualScreenWidth;
        double b = t + SystemParameters.VirtualScreenHeight;
        // The title bar must land somewhere usable.
        return left >= l - 50 && left <= r - 120 && top >= t - 5 && top <= b - 40;
    }

    private void SaveBounds()
    {
        var w = _config.Current.Window;
        if (WindowState == WindowState.Maximized)
        {
            var r = RestoreBounds; // the normal (un-maximized) size
            if (r.Width > 0 && r.Height > 0)
            {
                w.Width = r.Width;
                w.Height = r.Height;
                w.Left = r.Left;
                w.Top = r.Top;
            }
            w.Maximized = true;
        }
        else if (WindowState == WindowState.Normal && ActualWidth > 0 && ActualHeight > 0)
        {
            w.Width = ActualWidth;
            w.Height = ActualHeight;
            w.Left = Left;
            w.Top = Top;
            w.Maximized = false;
        }
        _config.Save();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).ExitApplication();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveBounds();

        // Closing hides to the tray unless the app is really shutting down.
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
