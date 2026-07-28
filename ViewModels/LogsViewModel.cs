using CommunityToolkit.Mvvm.ComponentModel;
using IRacingSmartPlug.Services;

namespace IRacingSmartPlug.ViewModels;

public sealed partial class LogsViewModel : ObservableObject
{
    private readonly LogService _log;
    private int _lastRevision = -1;

    [ObservableProperty] private string _logText = "";

    public LogsViewModel(LogService log) => _log = log;

    public void Refresh()
    {
        if (_log.Revision == _lastRevision) return;
        _lastRevision = _log.Revision;
        LogText = _log.Snapshot();
    }
}
