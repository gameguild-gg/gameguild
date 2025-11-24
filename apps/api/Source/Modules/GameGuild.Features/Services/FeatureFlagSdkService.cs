using System.Security.Cryptography;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;

namespace GameGuild.Features.Services;

/// <summary>
///     Implementation of SDK configuration generation
/// </summary>
public class FeatureFlagSdkService(IFeatureFlagRepository repository, IFeatureFlagEncryptionService encryptionService) : IFeatureFlagSdkService
{
    public async Task<SdkConfiguration> GenerateSdkConfigurationAsync(string environment, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var flags = await repository.GetByEnvironmentAsync(environment);

        if (!string.IsNullOrEmpty(tenantId)) { flags = flags.Where(f => f.TenantId == Guid.Parse(tenantId) || f.IsGlobal).ToList(); }

        // Return basic SDK configuration
        return new SdkConfiguration
        {
            Environment = environment, BaseUrl = "/api/features", TimeoutSeconds = 30, PollingIntervalSeconds = 60, EnableCaching = true, CacheExpirationMinutes = 5, EnableAnalytics = true, EnableDebugLogging = false
        };
    }

    public Task<SdkEndpoints> GetSdkEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new SdkEndpoints { Features = "/api/features", Evaluate = "/api/features/evaluate", Analytics = "/api/features/analytics", Health = "/health", Config = "/api/sdk/config" };

        return Task.FromResult(endpoints);
    }

    public Task<string> GenerateApiKeyAsync(string environment, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Generate a secure API key
        var keyBytes = new byte[32];

        using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(keyBytes); }

        var apiKey = $"{environment}_{Convert.ToBase64String(keyBytes)}";

        return Task.FromResult(apiKey);
    }

    public Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // Basic validation - check format
        if (string.IsNullOrWhiteSpace(apiKey)) { return Task.FromResult(false); }

        var parts = apiKey.Split('_');

        if (parts.Length != 2) { return Task.FromResult(false); }

        // Validate base64 portion
        try
        {
            Convert.FromBase64String(parts[1]);

            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }
}
