using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace IRacingSmartPlug.Services;

/// <summary>
/// Lightweight in-memory + file logger. Thread-safe; the UI polls
/// <see cref="Snapshot"/> to render the log view.
/// </summary>
public sealed class LogService
{
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly string _logFile;
    private readonly object _fileLock = new();
    private const int MaxLines = 500;

    public int Revision { get; private set; }

    public LogService()
    {
        var dir = ConfigService.DataDirectory;
        Directory.CreateDirectory(dir);
        _logFile = Path.Combine(dir, "iracing_plug.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {level,-5} {message}";
        _lines.Enqueue(line);
        while (_lines.Count > MaxLines && _lines.TryDequeue(out _)) { }
        Revision++;

        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFile, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Never let logging crash the app.
        }
    }

    public string Snapshot()
    {
        var sb = new StringBuilder();
        foreach (var l in _lines)
            sb.AppendLine(l);
        return sb.ToString();
    }
}
