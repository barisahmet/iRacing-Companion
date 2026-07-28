using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace IRacingSmartPlug.Services;

/// <summary>
/// Talks to Home Assistant's REST API to read and set the smart-plug state.
/// Reads live settings from <see cref="ConfigService"/> on every call so config
/// edits take effect immediately.
/// </summary>
public sealed class HomeAssistantService
{
    private readonly ConfigService _config;
    private readonly LogService _log;
    private readonly HttpClient _http = new();

    public string Status { get; private set; } = "starting";

    public HomeAssistantService(ConfigService config, LogService log)
    {
        _config = config;
        _log = log;
    }

    private (string url, string token, string entity, string domain, TimeSpan timeout) Read()
    {
        var ha = _config.Current.HomeAssistant;
        var beh = _config.Current.Behavior;
        var baseUrl = ha.BaseUrl.TrimEnd('/');
        var entity = string.IsNullOrWhiteSpace(ha.EntityId) ? "switch.racing_plug" : ha.EntityId;
        var domain = entity.Contains('.') ? entity.Split('.')[0] : "switch";
        return (baseUrl, ha.Token, entity, domain, TimeSpan.FromSeconds(Math.Max(1, beh.RequestTimeoutSeconds)));
    }

    private HttpRequestMessage Build(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            req.Content = JsonContent.Create(body);
        return req;
    }

    /// <summary>Read the plug state (also validates the token). Returns on/off/null.</summary>
    public async Task<bool?> GetStateAsync(CancellationToken ct = default)
    {
        var (url, token, entity, _, timeout) = Read();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using var resp = await _http.SendAsync(
                Build(HttpMethod.Get, $"{url}/api/states/{entity}", token), cts.Token);

            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                Status = "unauthorized (bad token)";
                _log.Error($"Home Assistant rejected the token (HTTP {(int)resp.StatusCode})");
                return null;
            }
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cts.Token));
            var raw = doc.RootElement.TryGetProperty("state", out var s) ? s.GetString() ?? "" : "";
            Status = "connected";
            _log.Info($"Home Assistant OK - {entity} = {(string.IsNullOrEmpty(raw) ? "unknown" : raw)}");
            return raw.ToLowerInvariant() switch { "on" => true, "off" => false, _ => null };
        }
        catch (Exception ex)
        {
            Status = "unreachable";
            _log.Warn($"Could not read plug state: {ex.Message}");
            return null;
        }
    }

    /// <summary>Turn the plug on or off.</summary>
    public async Task<bool> SetAsync(bool on, CancellationToken ct = default)
    {
        var (url, token, entity, domain, timeout) = Read();
        var service = on ? "turn_on" : "turn_off";
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using var resp = await _http.SendAsync(
                Build(HttpMethod.Post, $"{url}/api/services/{domain}/{service}", token,
                      new { entity_id = entity }), cts.Token);
            resp.EnsureSuccessStatusCode();
            _log.Info($"Plug -> {(on ? "ON" : "OFF")} (HTTP {(int)resp.StatusCode})");
            Status = "connected";
            return true;
        }
        catch (Exception ex)
        {
            _log.Warn($"Could not set plug {(on ? "ON" : "OFF")}: {ex.Message}");
            return false;
        }
    }
}
