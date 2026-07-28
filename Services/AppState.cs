namespace IRacingSmartPlug.Services;

/// <summary>
/// Snapshot of live runtime state, updated by the orchestrator on its background
/// task and polled by the UI on a dispatcher timer. Guarded by a simple lock.
/// </summary>
public sealed class AppState
{
    private readonly object _lock = new();

    private bool? _plug;
    private string _haStatus = "starting";
    private bool _uiRunning;
    private bool _simRunning;
    private HashSet<string> _running = new(StringComparer.OrdinalIgnoreCase);

    public void Update(bool? plug, string haStatus, bool uiRunning, bool simRunning, HashSet<string> running)
    {
        lock (_lock)
        {
            _plug = plug;
            _haStatus = haStatus;
            _uiRunning = uiRunning;
            _simRunning = simRunning;
            _running = running;
        }
    }

    public void SetPlug(bool? plug)
    {
        lock (_lock) { _plug = plug; }
    }

    public Snapshot Read()
    {
        lock (_lock)
        {
            return new Snapshot(_plug, _haStatus, _uiRunning, _simRunning, _running);
        }
    }

    public readonly record struct Snapshot(
        bool? Plug, string HaStatus, bool UiRunning, bool SimRunning, HashSet<string> Running);
}
