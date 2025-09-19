using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Core.Services;

/// <summary>
/// Service for managing rate limiting policies per endpoint, user, and IP
/// Supports both in-memory and distributed (Redis) rate limiting
/// </summary>
public interface IRateLimitingService {
  Task<RateLimitCheckResult> CheckRateLimitAsync(HttpContext context, string endpoint);
  Task RecordRequestAsync(HttpContext context, string endpoint);
  Task<Dictionary<string, object>> GetRateLimitStatsAsync(string? userId = null, string? ipAddress = null);
}

public class RateLimitingService : IRateLimitingService {
  private readonly RateLimitingOptions _options;
  private readonly IMemoryCache _memoryCache;
  private readonly IDistributedCache? _distributedCache;
  private readonly ILogger<RateLimitingService> _logger;

  public RateLimitingService(
    IOptions<RateLimitingOptions> options,
    IMemoryCache memoryCache,
    IDistributedCache? distributedCache,
    ILogger<RateLimitingService> logger) {
    _options = options.Value;
    _memoryCache = memoryCache;
    _distributedCache = distributedCache;
    _logger = logger;
  }

  public async Task<RateLimitCheckResult> CheckRateLimitAsync(HttpContext context, string endpoint) {
    var userId = GetUserId(context);
    var ipAddress = GetClientIpAddress(context);
    var userAgent = context.Request.Headers.UserAgent.ToString();

    _logger.LogDebug("Checking rate limit for endpoint: {Endpoint}, User: {UserId}, IP: {IpAddress}",
      endpoint, userId ?? "Anonymous", ipAddress);

    // Check if path is exempt
    if (IsExemptPath(context.Request.Path)) {
      return RateLimitCheckResult.Allow();
    }

    // Get endpoint-specific configuration
    var endpointConfig = GetEndpointConfig(endpoint);

    // Check multiple rate limit scopes
    var checks = new List<Task<RateLimitCheckResult>> {
      CheckGlobalRateLimit(ipAddress),
      CheckIPRateLimit(ipAddress, endpointConfig),
    };

    if (!string.IsNullOrEmpty(userId)) {
      checks.Add(CheckUserRateLimit(userId, endpoint, endpointConfig));
    }

    checks.Add(CheckEndpointRateLimit(endpoint, endpointConfig));

    var results = await Task.WhenAll(checks);

    // Return the most restrictive result
    var mostRestrictive = results.OrderBy(r => r.IsAllowed ? 1 : 0)
                                 .ThenBy(r => r.RetryAfter?.TotalSeconds ?? 0)
                                 .First();

    if (!mostRestrictive.IsAllowed) {
      _logger.LogWarning("Rate limit exceeded for endpoint: {Endpoint}, User: {UserId}, IP: {IpAddress}, Reason: {Reason}",
        endpoint, userId ?? "Anonymous", ipAddress, mostRestrictive.Reason);
    }

    return mostRestrictive;
  }

  public async Task RecordRequestAsync(HttpContext context, string endpoint) {
    var userId = GetUserId(context);
    var ipAddress = GetClientIpAddress(context);

    if (IsExemptPath(context.Request.Path)) {
      return;
    }

    var endpointConfig = GetEndpointConfig(endpoint);

    var tasks = new List<Task> {
      RecordGlobalRequest(ipAddress),
      RecordIPRequest(ipAddress, endpointConfig),
      RecordEndpointRequest(endpoint, endpointConfig)
    };

    if (!string.IsNullOrEmpty(userId)) {
      tasks.Add(RecordUserRequest(userId, endpoint, endpointConfig));
    }

    await Task.WhenAll(tasks);

    _logger.LogDebug("Recorded request for endpoint: {Endpoint}, User: {UserId}, IP: {IpAddress}",
      endpoint, userId ?? "Anonymous", ipAddress);
  }

  public async Task<Dictionary<string, object>> GetRateLimitStatsAsync(string? userId = null, string? ipAddress = null) {
    var stats = new Dictionary<string, object>();

    if (!string.IsNullOrEmpty(userId)) {
      stats["user"] = await GetUserStatsAsync(userId);
    }

    if (!string.IsNullOrEmpty(ipAddress)) {
      stats["ip"] = await GetIPStatsAsync(ipAddress);
    }

    stats["global"] = await GetGlobalStatsAsync();

    return stats;
  }

  private string? GetUserId(HttpContext context) {
    return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  }

  private string GetClientIpAddress(HttpContext context) {
    // Check for Cloudflare's real IP header first
    if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrEmpty(cfIp)) {
      return cfIp.ToString();
    }

    // Check for forwarded IP headers
    if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrEmpty(forwardedFor)) {
      var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
      if (ips.Length > 0) {
        return ips[0].Trim();
      }
    }

    if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrEmpty(realIp)) {
      return realIp.ToString();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
  }

  private bool IsExemptPath(string path) {
    return _options.ExemptPaths.Any(exemptPath =>
      path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase));
  }

  private EndpointRateLimitConfig GetEndpointConfig(string endpoint) {
    if (_options.EndpointSpecificLimits.TryGetValue(endpoint, out var config)) {
      return config;
    }

    // Return default configuration based on endpoint type
    return endpoint.ToLowerInvariant() switch {
      var e when e.Contains("/auth/") => new EndpointRateLimitConfig {
        RequestsPerMinute = _options.AuthRequestsPerMinute,
        BurstSize = _options.AuthRequestsPerMinute / 6 // ~10 seconds worth
      },
      var e when e.Contains("/graphql") => new EndpointRateLimitConfig {
        RequestsPerMinute = _options.GraphQLRequestsPerMinute,
        BurstSize = _options.GraphQLRequestsPerMinute / 4 // ~15 seconds worth
      },
      var e when e.Contains("/payment") => new EndpointRateLimitConfig {
        RequestsPerMinute = _options.PaymentRequestsPerMinute,
        BurstSize = _options.PaymentRequestsPerMinute / 5 // ~12 seconds worth
      },
      _ => new EndpointRateLimitConfig {
        RequestsPerMinute = _options.RequestsPerMinute,
        BurstSize = _options.BurstSize
      }
    };
  }

  private async Task<RateLimitCheckResult> CheckGlobalRateLimit(string ipAddress) {
    var key = $"global_ip:{ipAddress}";
    return await CheckRateLimit(key, _options.RequestsPerMinutePerIP, _options.BurstSizePerIP, "Global IP limit");
  }

  private async Task<RateLimitCheckResult> CheckIPRateLimit(string ipAddress, EndpointRateLimitConfig config) {
    if (!config.ApplyToIP) return RateLimitCheckResult.Allow();

    var key = $"ip:{ipAddress}";
    return await CheckRateLimit(key, config.RequestsPerMinute, config.BurstSize, "IP limit");
  }

  private async Task<RateLimitCheckResult> CheckUserRateLimit(string userId, string endpoint, EndpointRateLimitConfig config) {
    if (!config.ApplyToUser) return RateLimitCheckResult.Allow();

    var key = $"user:{userId}:{endpoint}";
    return await CheckRateLimit(key, config.RequestsPerMinute, config.BurstSize, "User endpoint limit");
  }

  private async Task<RateLimitCheckResult> CheckEndpointRateLimit(string endpoint, EndpointRateLimitConfig config) {
    var key = $"endpoint:{endpoint}";
    return await CheckRateLimit(key, config.RequestsPerMinute * 10, config.BurstSize * 10, "Endpoint global limit");
  }

  private async Task<RateLimitCheckResult> CheckRateLimit(string key, int requestsPerMinute, int burstSize, string reason) {
    if (_options.UseDistributedRateLimiting && _distributedCache != null) {
      return await CheckDistributedRateLimit(key, requestsPerMinute, burstSize, reason);
    }

    return CheckMemoryRateLimit(key, requestsPerMinute, burstSize, reason);
  }

  private RateLimitCheckResult CheckMemoryRateLimit(string key, int requestsPerMinute, int burstSize, string reason) {
    var window = _memoryCache.GetOrCreate($"{key}:window", entry => {
      entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
      entry.Size = 1; // Each rate limit window counts as 1 unit for memory management
      return new RateLimitWindow { TokensRemaining = burstSize };
    })!;

    lock (window) {
      var now = DateTimeOffset.UtcNow;

      // Reset window if needed
      if (now - window.WindowStart >= TimeSpan.FromMinutes(1)) {
        window.WindowStart = now;
        window.RequestCount = 0;
        window.TokensRemaining = burstSize;
      }

      // Check burst limit
      if (window.TokensRemaining <= 0) {
        var retryAfter = TimeSpan.FromMinutes(1) - (now - window.WindowStart);
        return RateLimitCheckResult.Deny(reason, retryAfter);
      }

      // Check rate limit
      if (window.RequestCount >= requestsPerMinute) {
        var retryAfter = TimeSpan.FromMinutes(1) - (now - window.WindowStart);
        return RateLimitCheckResult.Deny(reason, retryAfter);
      }

      return RateLimitCheckResult.Allow();
    }
  }

  private async Task<RateLimitCheckResult> CheckDistributedRateLimit(string key, int requestsPerMinute, int burstSize, string reason) {
    // Implementation for Redis-based distributed rate limiting
    // This would use Lua scripts for atomic operations
    // For now, fall back to memory-based
    return CheckMemoryRateLimit(key, requestsPerMinute, burstSize, reason);
  }

  private async Task RecordGlobalRequest(string ipAddress) {
    await RecordRequest($"global_ip:{ipAddress}", _options.RequestsPerMinutePerIP, _options.BurstSizePerIP);
  }

  private async Task RecordIPRequest(string ipAddress, EndpointRateLimitConfig config) {
    if (config.ApplyToIP) {
      await RecordRequest($"ip:{ipAddress}", config.RequestsPerMinute, config.BurstSize);
    }
  }

  private async Task RecordUserRequest(string userId, string endpoint, EndpointRateLimitConfig config) {
    if (config.ApplyToUser) {
      await RecordRequest($"user:{userId}:{endpoint}", config.RequestsPerMinute, config.BurstSize);
    }
  }

  private async Task RecordEndpointRequest(string endpoint, EndpointRateLimitConfig config) {
    await RecordRequest($"endpoint:{endpoint}", config.RequestsPerMinute * 10, config.BurstSize * 10);
  }

  private async Task RecordRequest(string key, int requestsPerMinute, int burstSize) {
    if (_options.UseDistributedRateLimiting && _distributedCache != null) {
      await RecordDistributedRequest(key, requestsPerMinute, burstSize);
      return;
    }

    RecordMemoryRequest(key, requestsPerMinute, burstSize);
  }

  private void RecordMemoryRequest(string key, int requestsPerMinute, int burstSize) {
    var window = _memoryCache.GetOrCreate($"{key}:window", entry => {
      entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
      entry.Size = 1; // Each rate limit window counts as 1 unit for memory management
      return new RateLimitWindow { TokensRemaining = burstSize };
    })!;

    lock (window) {
      var now = DateTimeOffset.UtcNow;

      // Reset window if needed
      if (now - window.WindowStart >= TimeSpan.FromMinutes(1)) {
        window.WindowStart = now;
        window.RequestCount = 0;
        window.TokensRemaining = burstSize;
      }

      window.RequestCount++;
      window.TokensRemaining = Math.Max(0, window.TokensRemaining - 1);
    }
  }

  private async Task RecordDistributedRequest(string key, int requestsPerMinute, int burstSize) {
    // Implementation for Redis-based distributed rate limiting
    // For now, fall back to memory-based
    RecordMemoryRequest(key, requestsPerMinute, burstSize);
    await Task.CompletedTask;
  }

  private async Task<object> GetUserStatsAsync(string userId) {
    // Return user-specific rate limit statistics
    return new { userId, message = "User stats not implemented yet" };
  }

  private async Task<object> GetIPStatsAsync(string ipAddress) {
    // Return IP-specific rate limit statistics
    return new { ipAddress, message = "IP stats not implemented yet" };
  }

  private async Task<object> GetGlobalStatsAsync() {
    // Return global rate limit statistics
    return new { message = "Global stats not implemented yet" };
  }
}

public class RateLimitCheckResult {
  public bool IsAllowed { get; private set; }
  public string? Reason { get; private set; }
  public TimeSpan? RetryAfter { get; private set; }

  private RateLimitCheckResult(bool isAllowed, string? reason = null, TimeSpan? retryAfter = null) {
    IsAllowed = isAllowed;
    Reason = reason;
    RetryAfter = retryAfter;
  }

  public static RateLimitCheckResult Allow() => new(true);
  public static RateLimitCheckResult Deny(string reason, TimeSpan? retryAfter = null) => new(false, reason, retryAfter);
}

public class RateLimitWindow {
  public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.UtcNow;
  public int RequestCount { get; set; } = 0;
  public int TokensRemaining { get; set; } = 0;
}
