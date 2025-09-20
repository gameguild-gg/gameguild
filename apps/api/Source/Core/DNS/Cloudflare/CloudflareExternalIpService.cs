using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.Configuration;
using GameGuild.Services;
using Microsoft.Extensions.Options;


namespace GameGuild.DNS.Cloudflare;

/// <summary>
/// Service that periodically checks external IP and updates Cloudflare DNS records.
/// Following Clean Architecture principles for infrastructure concerns
/// </summary>
public class CloudflareExternalIpService : ICloudflareExternalIpService, GameGuild.Services.ICloudflareExternalIpService, IDisposable {
  private readonly ILogger<CloudflareExternalIpService> _logger;

  private readonly CloudflareDynamicDnsOptions _options;

  private readonly HttpClient _httpClient;

  private readonly Timer? _timer;

  private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

  private bool _isRunning;

  private bool _isEnabled;

  private string? _lastKnownIp;

  private DateTime? _lastUpdate;

  private readonly Random _random = new Random();

  public CloudflareExternalIpService(ILogger<CloudflareExternalIpService> logger, IOptions<CloudflareDynamicDnsOptions> options, HttpClient httpClient) {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    // Configure HTTP client
    _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameGuild-DynamicDNS/1.0");

    // Validate configuration on startup
    ValidateConfiguration();

    // Initialize timer if service is enabled
    if (_isEnabled) {
      var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);
      _timer = new Timer(async _ => await UpdateExternalIpAsync(), null, TimeSpan.Zero, interval);
      _logger.LogInformation("Cloudflare Dynamic DNS service initialized with {IntervalMinutes} minute interval", _options.IntervalMinutes);
    }
  }

  public bool IsRunning => _isRunning;

  public string? LastKnownIp => _lastKnownIp;

  public DateTime? LastUpdate => _lastUpdate;

  public async Task StartAsync(CancellationToken cancellationToken = default) {
    if (!_isEnabled) {
      _logger.LogWarning("Cloudflare Dynamic DNS service is disabled in configuration");

      return;
    }

    if (_isRunning) {
      _logger.LogWarning("Cloudflare Dynamic DNS service is already running");

      return;
    }

    try {
      await _semaphore.WaitAsync(cancellationToken);
      _isRunning = true;
      _logger.LogInformation("Cloudflare Dynamic DNS service started");
    }
    finally { _semaphore.Release(); }
  }

  public async Task StopAsync(CancellationToken cancellationToken = default) {
    if (!_isRunning) { return; }

    try {
      await _semaphore.WaitAsync(cancellationToken);
      _isRunning = false;
      _timer?.Change(Timeout.Infinite, Timeout.Infinite);
      _logger.LogInformation("Cloudflare Dynamic DNS service stopped");
    }
    finally { _semaphore.Release(); }
  }

  public async Task UpdateExternalIpAsync(CancellationToken cancellationToken = default) {
    if (!_isEnabled || !_isRunning) { return; }

    try {
      await _semaphore.WaitAsync(cancellationToken);

      var currentIp = await GetExternalIpAsync(cancellationToken);

      if (string.IsNullOrWhiteSpace(currentIp)) {
        _logger.LogWarning("Failed to retrieve external IP address");

        return;
      }

      if (currentIp == _lastKnownIp) {
        _logger.LogDebug("External IP address unchanged: {IpAddress}", currentIp);

        return;
      }

      _logger.LogInformation("External IP address changed from {OldIp} to {NewIp}. Updating DNS records...", _lastKnownIp ?? "unknown", currentIp);

      var success = await UpdateCloudflareRecordsAsync(currentIp, cancellationToken);

      if (success) {
        _lastKnownIp = currentIp;
        _lastUpdate = DateTime.UtcNow;
        _logger.LogInformation("Successfully updated Cloudflare DNS records with IP: {IpAddress}", currentIp);
      }
      else { _logger.LogError("Failed to update Cloudflare DNS records"); }
    }
    catch (Exception ex) { _logger.LogError(ex, "Error during external IP update process"); }
    finally { _semaphore.Release(); }
  }

  public async Task<string?> GetExternalIpAsync(CancellationToken cancellationToken = default) {
    var services = GetShuffledIpServices();

    foreach (var service in services) {
      try {
        var response = await _httpClient.GetStringAsync(service, cancellationToken);
        var cleanIp = CleanIpAddress(response);

        if (IsValidIpAddress(cleanIp)) {
          _logger.LogDebug("Retrieved external IP {IpAddress} from service {Service}", cleanIp, service);

          return cleanIp;
        }
      }
      catch (Exception ex) { _logger.LogWarning(ex, "Failed to get IP from service {Service}", service); }
    }

    _logger.LogError("Failed to retrieve external IP from all services");

    return null;
  }

  #region Private Methods

  private void ValidateConfiguration() {
    _isEnabled = !string.IsNullOrWhiteSpace(_options.ApiToken) && !string.IsNullOrWhiteSpace(_options.ZoneId) && _options.DnsRecords != null && _options.DnsRecords.Count != 0;

    if (!_isEnabled) {
      _logger.LogWarning("Cloudflare Dynamic DNS service is disabled due to missing configuration");

      return;
    }

    if (_options.IntervalMinutes < 1) {
      _logger.LogWarning("Invalid interval configured, using default of 5 minutes");
      _options.IntervalMinutes = 5;
    }

    if (_options.TimeoutSeconds < 5) {
      _logger.LogWarning("Invalid timeout configured, using default of 10 seconds");
      _options.TimeoutSeconds = 10;
    }

    _logger.LogInformation("Cloudflare Dynamic DNS service configured for zone {ZoneId} with {RecordCount} records", _options.ZoneId, _options.DnsRecords.Count);
  }

  private List<string> GetShuffledIpServices() {
    var services = new List<string> { "https://api.ipify.org", "https://icanhazip.com", "https://ipecho.net/plain", "https://myexternalip.com/raw", "https://ifconfig.me/ip", "https://ident.me" };

    // Shuffle the list for load balancing
    return services.OrderBy(_ => _random.Next()).ToList();
  }

  private static string CleanIpAddress(string response) { return response.Trim().Split('\n')[0].Trim(); }

  private static bool IsValidIpAddress(string? ipAddress) { return !string.IsNullOrWhiteSpace(ipAddress) && IPAddress.TryParse(ipAddress, out _); }

  private async Task<bool> UpdateCloudflareRecordsAsync(string ipAddress, CancellationToken cancellationToken) {
    var allSuccess = true;

    foreach (var record in _options.DnsRecords) {
      try {
        var success = await UpdateSingleRecordAsync(record, ipAddress, cancellationToken);

        if (success) continue;

        allSuccess = false;
        _logger.LogError("Failed to update DNS record {RecordName}", record.Name);
      }
      catch (Exception ex) {
        allSuccess = false;
        _logger.LogError(ex, "Error updating DNS record {RecordName}", record.Name);
      }
    }

    return allSuccess;
  }

  private async Task<bool> UpdateSingleRecordAsync(DnsRecordConfiguration record, string ipAddress, CancellationToken cancellationToken) {
    try {
      // First, get the current record to obtain its ID
      var recordId = await GetRecordIdAsync(record.Name, cancellationToken);

      if (string.IsNullOrWhiteSpace(recordId)) {
        _logger.LogError("Could not find DNS record ID for {RecordName}", record.Name);

        return false;
      }

      // Update the record
      var updateUrl = $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/dns_records/{recordId}";
      var updatePayload = new { type = "A", name = record.Name, content = ipAddress, ttl = record.Ttl };

      var json = JsonSerializer.Serialize(updatePayload);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      var request = new HttpRequestMessage(HttpMethod.Put, updateUrl) { Content = content };
      request.Headers.Add("Authorization", $"Bearer {_options.ApiToken}");

      var response = await _httpClient.SendAsync(request, cancellationToken);
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) {
        _logger.LogError("Cloudflare API returned error {StatusCode} for record {RecordName}: {Response}", response.StatusCode, record.Name, responseBody);

        return false;
      }

      var result = JsonSerializer.Deserialize<CloudflareApiResponse>(responseBody);

      if (result?.Success == true) {
        _logger.LogInformation("Successfully updated DNS record {RecordName} to {IpAddress}", record.Name, ipAddress);

        return true;
      }

      _logger.LogError("Cloudflare API returned success=false for record {RecordName}: {Response}", record.Name, responseBody);

      return false;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Exception while updating DNS record {RecordName}", record.Name);

      return false;
    }
  }

  private async Task<string?> GetRecordIdAsync(string recordName, CancellationToken cancellationToken) {
    try {
      var url = $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/dns_records?name={recordName}&type=A";
      var request = new HttpRequestMessage(HttpMethod.Get, url);
      request.Headers.Add("Authorization", $"Bearer {_options.ApiToken}");

      var response = await _httpClient.SendAsync(request, cancellationToken);
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode) {
        _logger.LogError("Failed to get record ID for {RecordName}. Status: {StatusCode}, Response: {Response}", recordName, response.StatusCode, responseBody);

        return null;
      }

      var result = JsonSerializer.Deserialize<CloudflareListResponse>(responseBody);

      if (result is { Success: true, Result.Count: > 0 }) { return result.Result[0].Id; }

      _logger.LogWarning("No DNS record found with name {RecordName}", recordName);

      return null;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Exception while getting record ID for {RecordName}", recordName);

      return null;
    }
  }

  #endregion

  #region IDisposable

  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    if (disposing) {
      _timer?.Dispose();
      _semaphore?.Dispose();
    }
  }

  #endregion
}
