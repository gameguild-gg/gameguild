using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Cache management endpoints for testing and administration
/// </summary>
internal class CacheEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cache").WithTags("Cache").WithDescription("Cache management operations");

        group.MapGet("/health", GetCacheHealth).WithName("GetCacheHealth").WithSummary("Check cache health").WithDescription("Returns the health status of the Redis cache");

        group.MapPost("/test", TestCacheOperations).WithName("TestCacheOperations").WithSummary("Test cache operations").WithDescription("Test basic cache set/get/delete operations");

        group.MapDelete(
                "/clear/{pattern}",
                (string pattern, [FromServices] ICacheService cacheService, CancellationToken cancellationToken) =>
                    CacheEndpointHandlers.ClearCacheByPattern(pattern, cacheService, cancellationToken)
            )
            .WithName("ClearCacheByPattern")
            .WithSummary("Clear cache by pattern")
            .WithDescription("Clear cache entries matching the specified pattern");

        group.MapGet("/stats", GetCacheStats).WithName("GetCacheStats").WithSummary("Get cache statistics").WithDescription("Returns cache usage statistics");
    }

    private static async Task<IResult> GetCacheHealth([FromServices] ICacheService cacheService)
    {
        try
        {
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = "health_check_value";

            // Test cache operations
            await cacheService.SetAsync(testKey, testValue, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            var retrievedValue = await cacheService.GetAsync<string>(testKey).ConfigureAwait(false);
            await cacheService.RemoveAsync(testKey).ConfigureAwait(false);

            var isHealthy = retrievedValue == testValue;

            return Results.Ok(new { Status = isHealthy ? "Healthy" : "Unhealthy", Message = isHealthy ? "Cache operations successful" : "Cache operations failed", Timestamp = DateTimeOffset.UtcNow });
        }
        catch (InvalidOperationException ex) { return Results.Ok(new { Status = "Unhealthy", Message = $"Cache configuration error: {ex.Message}", Timestamp = DateTimeOffset.UtcNow }); }
        catch (TimeoutException ex) { return Results.Ok(new { Status = "Unhealthy", Message = $"Cache timeout: {ex.Message}", Timestamp = DateTimeOffset.UtcNow }); }
    }

    private static async Task<IResult> TestCacheOperations([FromServices] ICacheService cacheService)
    {
        try
        {
            var results = new List<object>();
            var testKey = $"test_{Guid.NewGuid()}";
            var testData = new { Message = "Test cache data", Timestamp = DateTimeOffset.UtcNow };

            // Test Set operation
            await cacheService.SetAsync(testKey, testData, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            results.Add(new { Operation = "Set", Status = "Success", Key = testKey });

            // Test Get operation
            var retrievedData = await cacheService.GetAsync<object>(testKey).ConfigureAwait(false);
            results.Add(new { Operation = "Get", Status = retrievedData != null ? "Success" : "Failed", Data = retrievedData });

            // Test Exists operation - check if data exists by trying to get it
            var existsData = await cacheService.GetAsync<object>(testKey).ConfigureAwait(false);
            var exists = existsData != null;
            results.Add(new { Operation = "Exists", Status = exists ? "Success" : "Failed", Exists = exists });

            // Test Remove operation
            await cacheService.RemoveAsync(testKey).ConfigureAwait(false);
            var existsAfterRemove = await cacheService.GetAsync<object>(testKey).ConfigureAwait(false) != null;
            results.Add(new { Operation = "Remove", Status = !existsAfterRemove ? "Success" : "Failed", Exists = existsAfterRemove });

            return Results.Ok(new { Message = "Cache operations test completed", Results = results, Timestamp = DateTimeOffset.UtcNow });
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { Message = "Cache operations test failed", Error = ex.Message, Timestamp = DateTimeOffset.UtcNow }); }
    }

    private static Task<IResult> GetCacheStats()
    {
        // This is a simplified stats implementation
        // In a real scenario, you'd collect metrics from Redis
        var result = Results.Ok(
            new
            {
                Message = "Cache statistics",
                Stats = new
                {
                    Status = "Available", Type = "Redis"
                    // Add more stats here when Redis metrics are available
                },
                Timestamp = DateTimeOffset.UtcNow
            }
        );

        return Task.FromResult(result);
    }
}

/// <summary>
///     Testable handlers for cache management endpoints.
/// </summary>
public static class CacheEndpointHandlers
{
    /// <summary>
    ///     Clears cache entries matching the supplied wildcard pattern when the configured cache provider supports it.
    /// </summary>
    public static async Task<IResult> ClearCacheByPattern(string pattern, ICacheService cacheService, CancellationToken cancellationToken = default)
    {
        if (cacheService is not IPatternCacheService patternCacheService)
            return Results.BadRequest(
                new
                {
                    Message = "The configured cache service does not support pattern-based cache clearing.",
                    Pattern = pattern,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );

        try
        {
            var removedCount = await patternCacheService.RemoveByPatternAsync(pattern, cancellationToken).ConfigureAwait(false);

            return Results.Ok(
                new
                {
                    Message = "Cache entries cleared",
                    Pattern = pattern,
                    RemovedCount = removedCount,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    Message = "Failed to clear cache",
                    Pattern = pattern,
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new
                {
                    Message = "Failed to clear cache",
                    Pattern = pattern,
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }
        catch (NotSupportedException ex)
        {
            return Results.BadRequest(
                new
                {
                    Message = "Failed to clear cache",
                    Pattern = pattern,
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow,
                }
            );
        }
    }
}
