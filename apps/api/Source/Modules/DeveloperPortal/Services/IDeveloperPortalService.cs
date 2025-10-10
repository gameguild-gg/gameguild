using GameGuild.Modules.DeveloperPortal.Entities;

namespace GameGuild.Modules.DeveloperPortal.Services;

public interface IDeveloperPortalService
{
    Task<ApiKey> GenerateApiKeyAsync(Guid developerId, string name, Guid? tenantId, List<string>? scopes = null, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
    Task<ApiKey> RotateApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
    Task<bool> ValidateApiKeyAsync(string keyValue, CancellationToken cancellationToken = default);
    Task<List<ApiKey>> GetApiKeysByDeveloperAsync(Guid developerId, bool includeRevoked = false, CancellationToken cancellationToken = default);
    Task LogApiUsageAsync(Guid apiKeyId, string endpoint, string method, int statusCode, long responseTimeMs, CancellationToken cancellationToken = default);
    Task<ApiUsageStatsDto> GetApiUsageStatsAsync(Guid developerId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<ApiUsageLog>> GetApiUsageLogsAsync(Guid developerId, DateTime? startDate = null, DateTime? endDate = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}

public record ApiUsageStatsDto(
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    double AverageResponseTimeMs,
    Dictionary<string, int> EndpointUsage,
    Dictionary<int, int> StatusCodeDistribution,
    Dictionary<string, int> DailyRequestCount
);
