using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IRacingSmartPlug.Models;

namespace IRacingSmartPlug.Services;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> as JSON in %APPDATA%. On first run,
/// migrates settings from the old Python config.ini if it can be found.
/// </summary>
public sealed class ConfigService
{
    public static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "iRacingSmartPlug");

    private static string ConfigPath => Path.Combine(DataDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LogService _log;

    public AppConfig Current { get; private set; } = new();

    public event Action? Changed;

    public ConfigService(LogService log)
    {
        _log = log;
        Load();
    }

    public void Load()
    {
        Directory.CreateDirectory(DataDirectory);
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                Current = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
                _log.Info("Configuration loaded");
                return;
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to read config.json: {ex.Message}");
            }
        }

        // First run: try to migrate from the old Python config.ini.
        Current = TryMigrateFromIni() ?? new AppConfig();
        Save();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, JsonOpts));
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save config: {ex.Message}");
        }
    }

    // ---- migration from the legacy Python config.ini -------------------- //
    private AppConfig? TryMigrateFromIni()
    {
        foreach (var candidate in IniCandidates())
        {
            if (!File.Exists(candidate))
                continue;
            try
            {
                var ini = ParseIni(candidate);
                var cfg = new AppConfig();

                if (ini.TryGetValue("homeassistant", out var ha))
                {
                    cfg.HomeAssistant.BaseUrl = ha.GetValueOrDefault("base_url", cfg.HomeAssistant.BaseUrl);
                    cfg.HomeAssistant.Token = ha.GetValueOrDefault("token", "");
                    cfg.HomeAssistant.EntityId = ha.GetValueOrDefault("entity_id", cfg.HomeAssistant.EntityId);
                }
                if (ini.TryGetValue("behavior", out var beh))
                {
                    cfg.Behavior.UiProcessName = beh.GetValueOrDefault("process_name", cfg.Behavior.UiProcessName);
                    cfg.Behavior.PollIntervalSeconds = ParseInt(beh, "poll_interval_seconds", 5);
                    cfg.Behavior.OffDelaySeconds = ParseInt(beh, "off_delay_seconds", 150);
                    cfg.Behavior.RequestTimeoutSeconds = ParseInt(beh, "request_timeout_seconds", 10);
                }

                // Apps: new-style [app:*] first, else legacy companion sections.
                var appSections = ini.Keys.Where(k => k.StartsWith("app:", StringComparison.OrdinalIgnoreCase)).ToList();
                if (appSections.Count > 0)
                {
                    foreach (var sec in appSections)
                        cfg.Apps.Add(AppFromIni(ini[sec], sec.Substring(4)));
                }
                else
                {
                    foreach (var sec in new[] { "companion", "overlay_companion" })
                    {
                        if (ini.TryGetValue(sec, out var s) && s.TryGetValue("path", out var p) && !string.IsNullOrWhiteSpace(p))
                            cfg.Apps.Add(AppFromIni(s, Path.GetFileNameWithoutExtension(p)));
                    }
                }

                _log.Info($"Migrated settings from {candidate}");
                return cfg;
            }
            catch (Exception ex)
            {
                _log.Warn($"Could not migrate {candidate}: {ex.Message}");
            }
        }
        _log.Info("No legacy config found; starting with defaults");
        return null;
    }

    private static IEnumerable<string> IniCandidates()
    {
        yield return @"E:\Documents\Python\config.ini";
        yield return Path.Combine(AppContext.BaseDirectory, "config.ini");
    }

    private static ManagedApp AppFromIni(Dictionary<string, string> s, string fallbackName)
    {
        var path = s.GetValueOrDefault("path", "");
        return new ManagedApp
        {
            Name = string.IsNullOrWhiteSpace(fallbackName) ? Path.GetFileNameWithoutExtension(path) : fallbackName,
            Path = path,
            ProcessName = s.GetValueOrDefault("process_name", ""),
            Enabled = ParseBool(s, "enabled", false),
            StartMinimized = ParseBool(s, "start_minimized", false),
            Trigger = ParseBool(s, "launch_with_iracing", true) ? LaunchTrigger.IRacingUi : LaunchTrigger.IRacingUi
        };
    }

    private static int ParseInt(Dictionary<string, string> s, string key, int def) =>
        s.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : def;

    private static bool ParseBool(Dictionary<string, string> s, string key, bool def) =>
        s.TryGetValue(key, out var v) ? v.Trim().ToLowerInvariant() is "true" or "1" or "yes" or "on" : def;

    private static Dictionary<string, Dictionary<string, string>> ParseIni(string file)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var current = "";
        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();
                result[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else if (current.Length > 0)
            {
                var eq = line.IndexOf('=');
                if (eq > 0)
                {
                    var key = line[..eq].Trim();
                    var val = line[(eq + 1)..].Trim();
                    result[current][key] = val;
                }
            }
        }
        return result;
    }
}
