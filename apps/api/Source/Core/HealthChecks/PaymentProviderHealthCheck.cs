using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Health check for payment providers
/// </summary>
public class PaymentProviderHealthCheck : IHealthCheck {
    private readonly ILogger<PaymentProviderHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public PaymentProviderHealthCheck(ILogger<PaymentProviderHealthCheck> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var results = new List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)>();

        // Check Stripe (example)
        await CheckStripeHealth(results, cancellationToken);

        // Check other payment providers as needed
        // await CheckPayPalHealth(results, cancellationToken);

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

        if (healthyCount == 0) {
            return HealthCheckResult.Unhealthy("No payment providers are healthy", null, data);
        }
        else if (healthyCount < totalCount) {
            return HealthCheckResult.Degraded($"Only {healthyCount}/{totalCount} payment providers are healthy", null, data);
        }
        else {
            return HealthCheckResult.Healthy("All payment providers are healthy", data);
        }
    }

    private async Task CheckStripeHealth(List<(string Provider, bool IsHealthy, string? Error, long ResponseTime)> results, CancellationToken cancellationToken) {
        try {
            var stopwatch = Stopwatch.StartNew();

            // Simple HTTP check to Stripe's status page or API
            using var response = await _httpClient.GetAsync("https://status.stripe.com/api/v2/status.json", cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode) {
                results.Add(("Stripe", true, null, stopwatch.ElapsedMilliseconds));
            }
            else {
                results.Add(("Stripe", false, $"HTTP {response.StatusCode}", stopwatch.ElapsedMilliseconds));
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Stripe health check failed");
            results.Add(("Stripe", false, ex.Message, 0));
        }
    }
}