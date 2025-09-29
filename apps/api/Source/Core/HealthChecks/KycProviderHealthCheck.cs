using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Health check for KYC providers
/// </summary>
public class KycProviderHealthCheck : IHealthCheck {
    private readonly ILogger<KycProviderHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public KycProviderHealthCheck(ILogger<KycProviderHealthCheck> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var results = new List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)>();

        // Check various KYC providers
        // For now, we'll simulate checks - replace with actual provider health endpoints
        await CheckKycProviderHealth("Provider1", "https://example.com/health", results, cancellationToken);

        var healthyCount = results.Count(r => r.IsHealthy);
        var totalCount = results.Count;

        var data = new Dictionary<string, object> {
            ["healthy_providers"] = healthyCount,
            ["total_providers"] = totalCount,
            ["providers"] = results.Select(r => new {
                name = r.Provider,
                healthy = r.IsHealthy,
                error = r.Error,
                response_time_ms = r.ResponseTime
            }).ToArray()
        };

        if (totalCount == 0) {
            return HealthCheckResult.Healthy("No KYC providers configured", data);
        }
        else if (healthyCount == 0) {
            return HealthCheckResult.Unhealthy("No KYC providers are healthy", null, data);
        }
        else if (healthyCount < totalCount) {
            return HealthCheckResult.Degraded($"Only {healthyCount}/{totalCount} KYC providers are healthy", null, data);
        }
        else {
            return HealthCheckResult.Healthy("All KYC providers are healthy", data);
        }
    }

    private async Task CheckKycProviderHealth(string providerName, string healthEndpoint, List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)> results, CancellationToken cancellationToken) {
        try {
            var stopwatch = Stopwatch.StartNew();

            using var response = await _httpClient.GetAsync(healthEndpoint, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode) {
                results.Add((providerName, true, null, stopwatch.ElapsedMilliseconds));
            }
            else {
                results.Add((providerName, false, $"HTTP {response.StatusCode}", stopwatch.ElapsedMilliseconds));
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "KYC provider {Provider} health check failed", providerName);
            results.Add((providerName, false, ex.Message, 0));
        }
    }
}