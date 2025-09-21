using Microsoft.AspNetCore.Http;
using GameGuild.Core.Services;

namespace GameGuild.Tests.Helpers;

/// <summary>
/// Mock implementation of IRateLimitingService for testing purposes
/// This allows tests to run without rate limiting interference
/// </summary>
public class MockRateLimitingService : IRateLimitingService {
  public Task<RateLimitCheckResult> CheckRateLimitAsync(HttpContext context, string endpoint) {
    // Always allow requests during testing
    return Task.FromResult(RateLimitCheckResult.Allow());
  }

  public Task RecordRequestAsync(HttpContext context, string endpoint) {
    // No-op for testing
    return Task.CompletedTask;
  }

  public Task<Dictionary<string, object>> GetRateLimitStatsAsync(string? userId = null, string? ipAddress = null) {
    // Return empty stats for testing
    return Task.FromResult(new Dictionary<string, object>());
  }
}