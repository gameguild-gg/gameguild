using System.Net;
using System.Text;
using System.Text.Json;
using GameGuild.Common.Configuration;
using GameGuild.Common.Models.Cloudflare;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GameGuild.Common.Services;

/// <summary>
/// Service that periodically checks external IP and updates Cloudflare DNS records.
/// </summary>
public class CloudflareExternalIpService : ICloudflareExternalIpService, IDisposable {
    private readonly ILogger<CloudflareExternalIpService> _logger;
    private readonly CloudflareDynamicDnsOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Timer? _timer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private bool _isRunning;
    private bool _isEnabled;
    private string? _lastKnownIp;
    private DateTime? _lastUpdate;
    private int _currentServiceIndex = 0;
    private readonly Random _random = new();

    public CloudflareExternalIpService(
      ILogger<CloudflareExternalIpService> logger,
      IOptions<CloudflareDynamicDnsOptions> options,
      HttpClient httpClient) {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;

        // Configure HTTP client
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameGuild-DynamicDNS/1.0");

        // Validate configuration on startup
        ValidateConfiguration();

        // Initialize timer if service is enabled
        if (_isEnabled) {
            var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);
            _timer = new Timer(async _ => await UpdateExternalIpAsync(), null, TimeSpan.Zero, interval);
            _logger.LogInformation("Cloudflare Dynamic DNS service initialized with {IntervalMinutes} minute interval",
              _options.IntervalMinutes);
        }
    }

    public bool IsRunning => _isRunning;
    public string? LastKnownIp => _lastKnownIp;
    public DateTime? LastUpdate => _lastUpdate;

    public async Task StartAsync(CancellationToken cancellationToken = default) {
        if (!_isEnabled) {
            _logger.LogWarning("Cloudflare Dynamic DNS service is disabled due to configuration issues");
            return;
        }

        _isRunning = true;
        _logger.LogInformation("Cloudflare Dynamic DNS service started");

        // Perform initial update
        await UpdateExternalIpAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) {
        _isRunning = false;
        _timer?.Dispose();
        _logger.LogInformation("Cloudflare Dynamic DNS service stopped");
        return Task.CompletedTask;
    }

    public async Task UpdateExternalIpAsync(CancellationToken cancellationToken = default) {
        if (!_isEnabled || !_isRunning) return;

        await _semaphore.WaitAsync(cancellationToken);
        try {
            _logger.LogDebug("Checking external IP address");

            var currentIp = await GetExternalIpAsync(cancellationToken);
            if (string.IsNullOrEmpty(currentIp)) {
                _logger.LogWarning("Failed to retrieve external IP address");
                return;
            }

            if (currentIp == _lastKnownIp) {
                _logger.LogDebug("External IP unchanged: {CurrentIp}", currentIp);
                return;
            }

            _logger.LogInformation("External IP changed from {OldIp} to {NewIp}", _lastKnownIp ?? "unknown", currentIp);

            // Update all configured DNS records
            var updateTasks = _options.DnsRecords.Select(record =>
              UpdateDnsRecordAsync(record, currentIp, cancellationToken));

            var results = await Task.WhenAll(updateTasks);

            if (results.All(success => success)) {
                _lastKnownIp = currentIp;
                _lastUpdate = DateTime.UtcNow;
                _logger.LogInformation("Successfully updated all DNS records with IP {CurrentIp}", currentIp);
            }
            else {
                _logger.LogWarning("Some DNS record updates failed");
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating external IP");
        }
        finally {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetExternalIpAsync(CancellationToken cancellationToken = default) {
        var enabledServices = _options.ExternalIpServices.Where(s => s.Enabled).ToList();

        if (!enabledServices.Any()) {
            _logger.LogError("No enabled external IP services configured");
            return null;
        }

        // Start from current service index and try all services
        for (int attempt = 0; attempt < enabledServices.Count; attempt++) {
            var service = enabledServices[_currentServiceIndex];
            var ip = await TryGetExternalIpFromServiceAsync(service, cancellationToken);

            if (!string.IsNullOrEmpty(ip)) {
                // Move to next service for next time (rotation)
                _currentServiceIndex = (_currentServiceIndex + 1) % enabledServices.Count;
                return ip;
            }

            // Move to next service and try again
            _currentServiceIndex = (_currentServiceIndex + 1) % enabledServices.Count;
            _logger.LogWarning("Failed to get IP from {ServiceName}, trying next service", service.Name);
        }

        _logger.LogError("All external IP services failed");
        return null;
    }

    private async Task<string?> TryGetExternalIpFromServiceAsync(ExternalIpServiceConfiguration service, CancellationToken cancellationToken) {
        try {
            _logger.LogDebug("Fetching external IP from {ServiceName} ({ServiceUrl})", service.Name, service.Url);

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(service.TimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "GameGuild-DynamicDNS/1.0");

            var response = await httpClient.GetStringAsync(service.Url, cancellationToken);

            string? ip = null;

            if (service.ResponseFormat == ExternalIpResponseFormat.PlainText) {
                ip = response.Trim();
            }
            else if (service.ResponseFormat == ExternalIpResponseFormat.Json && !string.IsNullOrEmpty(service.JsonPath)) {
                try {
                    var json = JObject.Parse(response);
                    var token = json.SelectToken(service.JsonPath);
                    ip = token?.ToString();
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to parse JSON response from {ServiceName} using path {JsonPath}",
                      service.Name, service.JsonPath);
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(ip)) {
                _logger.LogWarning("Empty or null IP received from {ServiceName}", service.Name);
                return null;
            }

            if (IPAddress.TryParse(ip, out _)) {
                _logger.LogDebug("Retrieved external IP from {ServiceName}: {ExternalIp}", service.Name, ip);
                return ip;
            }

            _logger.LogWarning("Invalid IP address received from {ServiceName}: {Response}", service.Name, ip);
            return null;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to retrieve external IP from {ServiceName} ({ServiceUrl})",
              service.Name, service.Url);
            return null;
        }
    }

    private async Task<bool> UpdateDnsRecordAsync(DnsRecordConfiguration config, string ipAddress, CancellationToken cancellationToken) {
        try {
            _logger.LogDebug("Updating DNS record {RecordName} ({RecordType}) with IP {IpAddress}",
              config.Name, config.Type, ipAddress);

            // First, get existing records to find the record ID
            var existingRecord = await GetDnsRecordAsync(config, cancellationToken);

            if (existingRecord != null) {
                // Check if the IP address is different before updating
                if (existingRecord.Content == ipAddress) {
                    _logger.LogDebug("DNS record {RecordName} ({RecordType}) already has IP {IpAddress}, skipping update",
                      config.Name, config.Type, ipAddress);
                    return true; // No update needed, but consider it successful
                }
                
                // Update existing record with new IP
                return await UpdateExistingDnsRecordAsync(existingRecord.Id, config, ipAddress, cancellationToken);
            }
            else {
                // Create new record
                return await CreateDnsRecordAsync(config, ipAddress, cancellationToken);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to update DNS record {RecordName} ({RecordType})", config.Name, config.Type);
            return false;
        }
    }

    private async Task<CloudflareDnsRecord?> GetDnsRecordAsync(DnsRecordConfiguration config, CancellationToken cancellationToken) {
        var url = $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/dns_records?name={config.Name}&type={config.Type}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {_options.ApiToken}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            _logger.LogError("Failed to get DNS records. Status: {StatusCode}, Response: {Response}",
              response.StatusCode, content);
            return null;
        }

        var apiResponse = JsonSerializer.Deserialize<CloudflareApiResponse<List<CloudflareDnsRecord>>>(content);

        if (apiResponse?.Success != true) {
            _logger.LogError("Cloudflare API returned errors: {Errors}",
              string.Join(", ", apiResponse?.Errors?.Select(e => e.Message) ?? new[] { "Unknown error" }));
            return null;
        }

        return apiResponse.Result?.FirstOrDefault();
    }

    private async Task<bool> UpdateExistingDnsRecordAsync(string recordId, DnsRecordConfiguration config, string ipAddress, CancellationToken cancellationToken) {
        var url = $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/dns_records/{recordId}";

        var requestData = new CloudflareDnsRecordRequest {
            Type = config.Type,
            Name = config.Name,
            Content = ipAddress,
            Ttl = config.Ttl,
            Proxied = config.Proxied,
            Comment = "Updated by GameGuild Dynamic DNS Service"
        };

        var json = JsonSerializer.Serialize(requestData);
        using var request = new HttpRequestMessage(HttpMethod.Put, url) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_options.ApiToken}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            _logger.LogError("Failed to update DNS record {RecordId}. Status: {StatusCode}, Response: {Response}",
              recordId, response.StatusCode, content);
            return false;
        }

        var apiResponse = JsonSerializer.Deserialize<CloudflareApiResponse<CloudflareDnsRecord>>(content);

        if (apiResponse?.Success != true) {
            _logger.LogError("Cloudflare API returned errors while updating record {RecordId}: {Errors}",
              recordId, string.Join(", ", apiResponse?.Errors?.Select(e => e.Message) ?? new[] { "Unknown error" }));
            return false;
        }

        _logger.LogInformation("Successfully updated DNS record {RecordName} ({RecordType}) with IP {IpAddress}",
          config.Name, config.Type, ipAddress);
        return true;
    }

    private async Task<bool> CreateDnsRecordAsync(DnsRecordConfiguration config, string ipAddress, CancellationToken cancellationToken) {
        var url = $"https://api.cloudflare.com/client/v4/zones/{_options.ZoneId}/dns_records";

        var requestData = new CloudflareDnsRecordRequest {
            Type = config.Type,
            Name = config.Name,
            Content = ipAddress,
            Ttl = config.Ttl,
            Proxied = config.Proxied,
            Comment = "Created by GameGuild Dynamic DNS Service"
        };

        var json = JsonSerializer.Serialize(requestData);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_options.ApiToken}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            _logger.LogError("Failed to create DNS record {RecordName}. Status: {StatusCode}, Response: {Response}",
              config.Name, response.StatusCode, content);
            return false;
        }

        var apiResponse = JsonSerializer.Deserialize<CloudflareApiResponse<CloudflareDnsRecord>>(content);

        if (apiResponse?.Success != true) {
            _logger.LogError("Cloudflare API returned errors while creating record {RecordName}: {Errors}",
              config.Name, string.Join(", ", apiResponse?.Errors?.Select(e => e.Message) ?? new[] { "Unknown error" }));
            return false;
        }

        _logger.LogInformation("Successfully created DNS record {RecordName} ({RecordType}) with IP {IpAddress}",
          config.Name, config.Type, ipAddress);
        return true;
    }

    private void ValidateConfiguration() {
        // Debug logging to see what's actually being bound
        _logger.LogInformation("Cloudflare Dynamic DNS configuration debug:");
        _logger.LogInformation("  Enabled: {Enabled}", _options.Enabled);
        _logger.LogInformation("  ApiToken: {ApiToken}", string.IsNullOrEmpty(_options.ApiToken) ? "NULL" : "SET");
        _logger.LogInformation("  ZoneId: {ZoneId}", string.IsNullOrEmpty(_options.ZoneId) ? "NULL" : "SET");
        _logger.LogInformation("  DnsRecords count: {Count}", _options.DnsRecords?.Count ?? 0);
        
        if (_options.DnsRecords?.Any() == true) {
            foreach (var (record, index) in _options.DnsRecords.Select((r, i) => (r, i))) {
                _logger.LogInformation("  DNS Record {Index}: Type={Type}, Name={Name}, TTL={Ttl}, Proxied={Proxied}",
                    index, record.Type, record.Name, record.Ttl, record.Proxied);
            }
        } else {
            _logger.LogWarning("  No DNS records found! This indicates a configuration binding issue.");
        }

        if (!_options.Enabled) {
            _logger.LogInformation("Cloudflare Dynamic DNS service is disabled");
            _isEnabled = false;
            return;
        }

        if (!_options.IsValid()) {
            var errors = _options.GetValidationErrors();
            _logger.LogWarning("Cloudflare Dynamic DNS service disabled due to configuration errors: {Errors}",
              string.Join("; ", errors));
            _isEnabled = false;
            return;
        }

        var enabledServices = _options.ExternalIpServices.Where(s => s.Enabled).ToList();
        if (!enabledServices.Any()) {
            _logger.LogWarning("Cloudflare Dynamic DNS service disabled: no enabled external IP services configured");
            _isEnabled = false;
            return;
        }

        _isEnabled = true;
        _logger.LogInformation("Cloudflare Dynamic DNS service configuration validated successfully. " +
                              "Will update {RecordCount} DNS records every {IntervalMinutes} minutes using {ServiceCount} IP services: {ServiceNames}",
          _options.DnsRecords.Count, _options.IntervalMinutes, enabledServices.Count,
          string.Join(", ", enabledServices.Select(s => s.Name)));
    }

    public void Dispose() {
        _timer?.Dispose();
        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
