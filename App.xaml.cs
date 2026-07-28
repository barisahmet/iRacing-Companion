using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using IRacingSmartPlug.Services;
using IRacingSmartPlug.ViewModels;
using IRacingSmartPlug.Views;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace IRacingSmartPlug;

public partial class App : Application
{
    private const string MutexName = "iRacingSmartPlug_SingleInstance_9f2a";
    private const string ShowEventName = "iRacingSmartPlug_Show_9f2a";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private ServiceProvider? _services;
    private PlugOrchestrator? _orchestrator;
    private TaskbarIcon? _tray;
    private MainWindow? _mainWindow;
    private DashboardViewModel? _dashboard;
    private ImageSource? _wheelIcon;
    private string? _trayOnPath;
    private string? _trayOffPath;

    public static bool IsExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tray app: stay alive with no window open; we exit explicitly.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // ---- single instance -------------------------------------------- //
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isNew);
        if (!isNew)
        {
            // Signal the running instance to show its window, then exit.
            try { EventWaitHandle.OpenExisting(ShowEventName).Set(); } catch { }
            Shutdown();
            return;
        }
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        StartShowListener();

        // ---- dependency injection --------------------------------------- //
        var sc = new ServiceCollection();
        sc.AddSingleton<LogService>();
        sc.AddSingleton<ConfigService>();
        sc.AddSingleton<HomeAssistantService>();
        sc.AddSingleton<ProcessMonitor>();
        sc.AddSingleton<AppLauncher>();
        sc.AddSingleton<AppState>();
        sc.AddSingleton<PlugOrchestrator>();
        sc.AddSingleton<StartupService>();
        sc.AddSingleton<IDialogService, DialogService>();
        sc.AddSingleton<DashboardViewModel>();
        sc.AddSingleton<AppsViewModel>();
        sc.AddSingleton<SettingsViewModel>();
        sc.AddSingleton<LogsViewModel>();
        sc.AddSingleton<MainViewModel>();
        sc.AddSingleton<MainWindow>();
        _services = sc.BuildServiceProvider();

        var log = _services.GetRequiredService<LogService>();
        log.Info("=== iRacing Companion (native) starting ===");

        // Keep the app alive on a stray UI exception, and record it.
        DispatcherUnhandledException += (_, args) =>
        {
            log.Error("UI exception: " + args.Exception);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            log.Error("Fatal: " + args.ExceptionObject);

        // ---- theme ------------------------------------------------------ //
        // Force the dark Fluent palette (light text on dark surfaces),
        // regardless of the Windows light/dark setting.
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        // ---- background orchestrator ------------------------------------ //
        _orchestrator = _services.GetRequiredService<PlugOrchestrator>();
        _orchestrator.Start();

        // ---- main window ------------------------------------------------ //
        _mainWindow = _services.GetRequiredService<MainWindow>();
        _mainWindow.DataContext = _services.GetRequiredService<MainViewModel>();
        SystemThemeWatcher.Watch(_mainWindow);

        // ---- tray ------------------------------------------------------- //
        BuildTray();

        // Green dot overlaid on the tray icon while iRacing is open.
        _dashboard = _services.GetRequiredService<MainViewModel>().Dashboard;
        _dashboard.PropertyChanged += OnDashboardChanged;
        UpdateTrayIcon(_dashboard.IRacingOpen);

        var startHidden = e.Args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
        if (startHidden)
            log.Info("Started minimized to tray");
        else
            ShowMainWindow();
    }

    private void BuildTray()
    {
        _tray = new TaskbarIcon { ToolTipText = "iRacing Companion" };

        try { _tray.IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/icon.ico")); }
        catch { }

        var menu = new ContextMenu();
        menu.Items.Add(MenuItem("Open", (_, _) => ShowMainWindow()));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Turn plug ON", (_, _) => _orchestrator?.RequestPlug(true)));
        menu.Items.Add(MenuItem("Turn plug OFF", (_, _) => _orchestrator?.RequestPlug(false)));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Quit", (_, _) => ExitApp()));
        _tray.ContextMenu = menu;
        _tray.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
        _tray.ForceCreate();
    }

    private static MenuItem MenuItem(string header, RoutedEventHandler onClick)
    {
        var mi = new MenuItem { Header = header };
        mi.Click += onClick;
        return mi;
    }

    private void OnDashboardChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IRacingOpen) && _dashboard is not null)
            UpdateTrayIcon(_dashboard.IRacingOpen);
    }

    // Tray icon = the wheel, plus a green dot in the lower-right while iRacing is open.
    // H.NotifyIcon loads icons from a URI, so we pre-render both states to PNG files.
    private void UpdateTrayIcon(bool iRacingOpen)
    {
        if (_tray is null) return;
        try
        {
            EnsureTrayFiles();
            _tray.IconSource = new BitmapImage(new Uri(iRacingOpen ? _trayOnPath! : _trayOffPath!));
            _tray.ToolTipText = iRacingOpen ? "iRacing Companion — iRacing open" : "iRacing Companion";
        }
        catch
        {
            // Never let a rendering hiccup take down the tray.
        }
    }

    private void EnsureTrayFiles()
    {
        if (_trayOnPath is not null) return;
        _wheelIcon ??= new BitmapImage(new Uri("pack://application:,,,/Assets/icon.ico"));
        var dir = ConfigService.DataDirectory;
        Directory.CreateDirectory(dir);
        _trayOffPath = System.IO.Path.Combine(dir, "tray_off.ico");
        _trayOnPath = System.IO.Path.Combine(dir, "tray_on.ico");
        RenderTrayFile(_trayOffPath, withDot: false);
        RenderTrayFile(_trayOnPath, withDot: true);
    }

    private void RenderTrayFile(string path, bool withDot)
    {
        const int s = 32;
        var group = new DrawingGroup();
        group.Children.Add(new ImageDrawing(_wheelIcon!, new Rect(0, 0, s, s)));
        if (withDot)
        {
            group.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84)),
                new Pen(new SolidColorBrush(Color.FromRgb(0x10, 0x13, 0x1A)), 2),
                new EllipseGeometry(new Point(s * 0.72, s * 0.72), s * 0.26, s * 0.26)));
        }

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            dc.DrawDrawing(group);

        var rtb = new RenderTargetBitmap(s, s, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        // Un-premultiply to straight BGRA, then write a 32-bit .ico.
        var straight = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
        var bgra = new byte[s * s * 4];
        straight.CopyPixels(bgra, s * 4, 0);
        WriteIco(path, bgra, s);
    }

    private static void WriteIco(string path, byte[] bgra, int size)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        int andStride = ((size + 31) / 32) * 4;
        int andSize = andStride * size;
        int imgSize = 40 + size * size * 4 + andSize;

        bw.Write((short)0);          // reserved
        bw.Write((short)1);          // type = icon
        bw.Write((short)1);          // image count
        bw.Write((byte)size);        // width
        bw.Write((byte)size);        // height
        bw.Write((byte)0);           // palette
        bw.Write((byte)0);           // reserved
        bw.Write((short)1);          // planes
        bw.Write((short)32);         // bpp
        bw.Write(imgSize);           // bytes in resource
        bw.Write(6 + 16);            // image offset

        bw.Write(40);                // biSize
        bw.Write(size);              // biWidth
        bw.Write(size * 2);          // biHeight (XOR + AND)
        bw.Write((short)1);          // biPlanes
        bw.Write((short)32);         // biBitCount
        bw.Write(0);                 // BI_RGB
        bw.Write(size * size * 4);   // biSizeImage
        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);

        for (int y = size - 1; y >= 0; y--)   // XOR bitmap, bottom-up
            bw.Write(bgra, y * size * 4, size * 4);
        for (int i = 0; i < andSize; i++)      // AND mask (alpha handles transparency)
            bw.Write((byte)0);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }

    private void StartShowListener()
    {
        var t = new Thread(() =>
        {
            while (!IsExiting)
            {
                if (_showEvent!.WaitOne(1000))
                    Dispatcher.Invoke(ShowMainWindow);
            }
        })
        { IsBackground = true, Name = "show-listener" };
        t.Start();
    }

    public void ExitApplication() => ExitApp();

    private void ExitApp()
    {
        IsExiting = true;
        try { _orchestrator?.Stop(); } catch { }
        try { _tray?.Dispose(); } catch { }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        try { _tray?.Dispose(); } catch { }
        try { _orchestrator?.Stop(); } catch { }
        try { _services?.Dispose(); } catch { }
        try { _mutex?.ReleaseMutex(); } catch { }
        base.OnExit(e);
    }
}
