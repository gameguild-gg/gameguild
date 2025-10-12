namespace GameGuild.Core.Services;

/// <summary>
/// Service for managing rate limiting policies per endpoint, user, and IP
/// Supports both in-memory and distributed (Redis) rate limiting
/// </summary>
public interface IRateLimitingService
{
    Task<RateLimitCheckResult> CheckRateLimitAsync(HttpContext context, string endpoint);

    Task RecordRequestAsync(HttpContext context, string endpoint);

    Task<Dictionary<string, object>> GetRateLimitStatsAsync(string? userId = null, string? ipAddress = null);
}
