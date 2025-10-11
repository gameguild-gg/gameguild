using System.Security.Cryptography;
using GameGuild.Modules.DeveloperPortal.Entities;
using GameGuild.Modules.DeveloperPortal.Repositories;


namespace GameGuild.Modules.DeveloperPortal.Services;

public class DeveloperPortalService : IDeveloperPortalService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiUsageLogRepository _apiUsageLogRepository;
    private readonly ILogger<DeveloperPortalService> _logger;

    public DeveloperPortalService(
        IApiKeyRepository apiKeyRepository,
        IApiUsageLogRepository apiUsageLogRepository,
        ILogger<DeveloperPortalService> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiUsageLogRepository = apiUsageLogRepository;
        _logger = logger;
    }

    public async Task<ApiKey> GenerateApiKeyAsync(
        Guid developerId,
        string name,
        Guid? tenantId,
        List<string>? scopes = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        var keyValue = GenerateSecureApiKey();
        var hashedKey = HashApiKey(keyValue);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            DeveloperId = developerId,
            TenantId = tenantId,
            Name = name,
            KeyHash = hashedKey,
            Scopes = scopes ?? new List<string>(),
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = null,
            UsageCount = 0,
            RateLimitPerMinute = 60,
            RateLimitPerHour = 1000,
            RateLimitPerDay = 10000
        };

        await _apiKeyRepository.AddAsync(apiKey, cancellationToken);

        _logger.LogInformation("Generated API key {KeyId} for developer {DeveloperId}", apiKey.Id, developerId);

        // Store the plain key value temporarily for return (won't be accessible again)
        apiKey.KeyHash = keyValue;

        return apiKey;
    }

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken);
        if (apiKey == null)
        {
            _logger.LogWarning("API key {KeyId} not found for revocation", apiKeyId);
            return false;
        }

        apiKey.Revoke();
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);

        _logger.LogInformation("Revoked API key {KeyId}", apiKeyId);
        return true;
    }

    public async Task<ApiKey> RotateApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var oldKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken);
        if (oldKey == null)
        {
            throw new InvalidOperationException($"API key {apiKeyId} not found");
        }

        // Revoke old key
        oldKey.Revoke();
        await _apiKeyRepository.UpdateAsync(oldKey, cancellationToken);

        // Generate new key with same settings
        var newKey = await GenerateApiKeyAsync(
            oldKey.DeveloperId,
            oldKey.Name,
            oldKey.TenantId,
            oldKey.Scopes.ToList(),
            oldKey.ExpiresAt,
            cancellationToken);

        _logger.LogInformation("Rotated API key {OldKeyId} to {NewKeyId}", apiKeyId, newKey.Id);

        return newKey;
    }

    public async Task<bool> ValidateApiKeyAsync(string keyValue, CancellationToken cancellationToken = default)
    {
        var hashedKey = HashApiKey(keyValue);
        var apiKey = await _apiKeyRepository.GetByHashAsync(hashedKey, cancellationToken);

        if (apiKey == null)
        {
            _logger.LogWarning("API key validation failed: key not found");
            return false;
        }

        if (!apiKey.IsActive)
        {
            _logger.LogWarning("API key validation failed: key {KeyId} is inactive", apiKey.Id);
            return false;
        }

        if (apiKey.IsRevoked)
        {
            _logger.LogWarning("API key validation failed: key {KeyId} is revoked", apiKey.Id);
            return false;
        }

        if (apiKey.IsExpired)
        {
            _logger.LogWarning("API key validation failed: key {KeyId} is expired", apiKey.Id);
            return false;
        }

        // Update last used timestamp
        apiKey.UpdateLastUsed();
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);

        return true;
    }

    public async Task<List<ApiKey>> GetApiKeysByDeveloperAsync(
        Guid developerId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        return await _apiKeyRepository.GetByDeveloperIdAsync(developerId, includeRevoked, cancellationToken);
    }

    public async Task LogApiUsageAsync(
        Guid apiKeyId,
        string endpoint,
        string method,
        int statusCode,
        long responseTimeMs,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken);
        if (apiKey == null)
        {
            _logger.LogWarning("Cannot log usage: API key {KeyId} not found", apiKeyId);
            return;
        }

        var usageLog = new ApiUsageLog
        {
            Id = Guid.NewGuid(),
            ApiKeyId = apiKeyId,
            Endpoint = endpoint,
            Method = method,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            RequestedAt = DateTime.UtcNow,
            IpAddress = null, // Set by middleware
            UserAgent = null  // Set by middleware
        };

        await _apiUsageLogRepository.AddAsync(usageLog, cancellationToken);

        // Increment usage count on API key
        apiKey.IncrementUsage();
        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
    }

    public async Task<ApiUsageStatsDto> GetApiUsageStatsAsync(
        Guid developerId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var apiKeys = await _apiKeyRepository.GetByDeveloperIdAsync(developerId, includeRevoked: true, cancellationToken);
        var apiKeyIds = apiKeys.Select(k => k.Id).ToList();

        var logs = await _apiUsageLogRepository.GetByApiKeyIdsAsync(apiKeyIds, startDate, endDate, cancellationToken);

        var totalRequests = logs.Count;
        var successfulRequests = logs.Count(l => l.StatusCode >= 200 && l.StatusCode < 300);
        var failedRequests = logs.Count(l => l.StatusCode >= 400);
        var averageResponseTime = logs.Any() ? logs.Average(l => l.ResponseTimeMs) : 0;

        var endpointUsage = logs
            .GroupBy(l => l.Endpoint)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusCodeDistribution = logs
            .GroupBy(l => l.StatusCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyRequestCount = logs
            .GroupBy(l => l.RequestedAt.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());

        return new ApiUsageStatsDto(
            totalRequests,
            successfulRequests,
            failedRequests,
            averageResponseTime,
            endpointUsage,
            statusCodeDistribution,
            dailyRequestCount
        );
    }

    public async Task<List<ApiUsageLog>> GetApiUsageLogsAsync(
        Guid developerId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var apiKeys = await _apiKeyRepository.GetByDeveloperIdAsync(developerId, includeRevoked: true, cancellationToken);
        var apiKeyIds = apiKeys.Select(k => k.Id).ToList();

        return await _apiUsageLogRepository.GetByApiKeyIdsAsync(apiKeyIds, startDate, endDate, skip, take, cancellationToken);
    }

    private static string GenerateSecureApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"gk_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private static string HashApiKey(string keyValue)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyValue));
        return Convert.ToBase64String(hashBytes);
    }
}
