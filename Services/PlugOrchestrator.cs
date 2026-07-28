using System.Threading.Channels;
using IRacingSmartPlug.Models;

namespace IRacingSmartPlug.Services;

/// <summary>
/// The heart of the app. Polls processes, powers the plug on/off around iRacing
/// sessions, and launches companion apps on their chosen trigger. Runs on a
/// background task; manual commands arrive over a channel.
/// </summary>
public sealed class PlugOrchestrator
{
    private readonly ConfigService _config;
    private readonly HomeAssistantService _ha;
    private readonly ProcessMonitor _procs;
    private readonly AppLauncher _launcher;
    private readonly AppState _state;
    private readonly LogService _log;

    private readonly Channel<Command> _commands = Channel.CreateUnbounded<Command>();
    private CancellationTokenSource? _cts;

    private bool _prevUi;
    private bool _prevSim;
    private bool _everActive;
    private DateTime _lastActive = DateTime.MinValue;

    public PlugOrchestrator(ConfigService config, HomeAssistantService ha, ProcessMonitor procs,
                            AppLauncher launcher, AppState state, LogService log)
    {
        _config = config;
        _ha = ha;
        _procs = procs;
        _launcher = launcher;
        _state = state;
        _log = log;
    }

    private abstract record Command;
    private sealed record SetPlugCommand(bool On) : Command;
    private sealed record RefreshCommand : Command;
    private sealed record LaunchCommand(string AppId) : Command;

    public void RequestPlug(bool on) => _commands.Writer.TryWrite(new SetPlugCommand(on));
    public void RequestRefresh() => _commands.Writer.TryWrite(new RefreshCommand());
    public void RequestLaunch(string appId) => _commands.Writer.TryWrite(new LaunchCommand(appId));

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop() => _cts?.Cancel();

    private async Task RunAsync(CancellationToken ct)
    {
        var beh = _config.Current.Behavior;
        _log.Info($"Watching UI='{beh.UiProcessName}' Sim='{beh.SimProcessName}' | " +
                  $"off-delay {beh.OffDelaySeconds}s | poll {beh.PollIntervalSeconds}s");

        await _ha.GetStateAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            await DrainCommandsAsync(ct);
            try
            {
                await TickAsync(ct);
            }
            catch (Exception ex)
            {
                _log.Error($"Loop error: {ex.Message}");
            }

            var poll = Math.Max(1, _config.Current.Behavior.PollIntervalSeconds);
            try
            {
                await WaitForCommandOrDelay(TimeSpan.FromSeconds(poll), ct);
            }
            catch (OperationCanceledException) { break; }
        }
        _log.Info("Orchestrator stopped");
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var beh = _config.Current.Behavior;
        var running = _procs.Snapshot();
        var uiRunning = ProcessMonitor.IsRunning(running, beh.UiProcessName);
        var simRunning = ProcessMonitor.IsRunning(running, beh.SimProcessName);

        var uiEdge = uiRunning && !_prevUi;
        var simEdge = simRunning && !_prevSim;
        _prevUi = uiRunning;
        _prevSim = simRunning;

        // Launch companion apps on their trigger's rising edge (dedup via running check).
        if (uiEdge) _log.Info("iRacing UI launch detected");
        if (simEdge) _log.Info("Simulator launch detected");

        if (uiEdge || simEdge)
        {
            foreach (var app in _config.Current.Apps.Where(a => a.Enabled))
            {
                var wantUi = app.Trigger is LaunchTrigger.IRacingUi or LaunchTrigger.Either;
                var wantSim = app.Trigger is LaunchTrigger.Simulator or LaunchTrigger.Either;
                if ((uiEdge && wantUi) || (simEdge && wantSim))
                    _launcher.Launch(app, running);
            }
        }

        // Decide plug power based on the configured plug trigger.
        var active = beh.PlugTrigger switch
        {
            PlugTrigger.IRacingUi => uiRunning,
            PlugTrigger.Simulator => simRunning,
            _ => uiRunning || simRunning
        };
        if (active)
        {
            _lastActive = DateTime.UtcNow;
            _everActive = true;
        }

        var current = _state.Read();
        var plug = current.Plug;

        if (active && plug is not true)
        {
            if (await _ha.SetAsync(true, ct)) plug = true;
        }
        else if (!active && _everActive && plug is true &&
                 (DateTime.UtcNow - _lastActive).TotalSeconds >= beh.OffDelaySeconds)
        {
            if (await _ha.SetAsync(false, ct)) plug = false;
        }

        _state.Update(plug, _ha.Status, uiRunning, simRunning, running);
    }

    private async Task DrainCommandsAsync(CancellationToken ct)
    {
        while (_commands.Reader.TryRead(out var cmd))
        {
            switch (cmd)
            {
                case SetPlugCommand s:
                    _log.Info($"Manual command: turn {(s.On ? "ON" : "OFF")}");
                    if (await _ha.SetAsync(s.On, ct)) _state.SetPlug(s.On);
                    break;
                case RefreshCommand:
                    _state.SetPlug(await _ha.GetStateAsync(ct));
                    break;
                case LaunchCommand l:
                    var app = _config.Current.Apps.FirstOrDefault(a => a.Id == l.AppId);
                    if (app is not null)
                    {
                        _log.Info($"Manual launch: {app.Name}");
                        _launcher.Launch(app, _procs.Snapshot(), force: true);
                    }
                    break;
            }
        }
    }

    private async Task WaitForCommandOrDelay(TimeSpan delay, CancellationToken ct)
    {
        // Wake early if a command arrives so manual actions feel instant.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var waitTask = _commands.Reader.WaitToReadAsync(timeoutCts.Token).AsTask();
        var delayTask = Task.Delay(delay, timeoutCts.Token);
        var done = await Task.WhenAny(waitTask, delayTask);
        timeoutCts.Cancel();
        try { await done; } catch (OperationCanceledException) { }
    }
}
