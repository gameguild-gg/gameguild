using MediatR;
using Microsoft.Extensions.Caching.Memory;


namespace GameGuild.Common.Behaviors;

/// <summary>
/// Caching behavior for read operations (queries) to improve performance
/// </summary>
public class CachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>, ICachedRequest
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        var requestName = typeof(TRequest).Name;

        // Try to get from cache first
        if (cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            logger.LogDebug("Cache hit for {RequestName} with key {CacheKey}", requestName, cacheKey);
            return cachedResponse!;
        }

        logger.LogDebug("Cache miss for {RequestName} with key {CacheKey}", requestName, cacheKey);

        // Execute the request
        var response = await next();

        // Cache the response if it's successful (for Result pattern)
        var shouldCache = ShouldCacheResponse(response);
        
        if (shouldCache)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = request.CacheExpiration,
                SlidingExpiration = request.SlidingExpiration,
                Priority = CacheItemPriority.Normal
            };

            cache.Set(cacheKey, response, cacheOptions);
            
            logger.LogDebug("Cached response for {RequestName} with key {CacheKey}, expires in {Expiration}",
                requestName, cacheKey, request.CacheExpiration);
        }

        return response;
    }

    private static bool ShouldCacheResponse(TResponse response)
    {
        // Don't cache null responses
        if (response == null)
            return false;

        // For Result pattern, only cache successful results
        if (response is Result result)
            return result.IsSuccess;

        // For Result<T> pattern, only cache successful results
        if (response.GetType().IsGenericType && 
            response.GetType().GetGenericTypeDefinition() == typeof(Result<>))
        {
            var isSuccessProperty = response.GetType().GetProperty("IsSuccess");
            if (isSuccessProperty != null)
            {
                return (bool)isSuccessProperty.GetValue(response)!;
            }
        }

        // Cache all other responses
        return true;
    }
}

/// <summary>
/// Interface for requests that support caching
/// </summary>
public interface ICachedRequest
{
    /// <summary>
    /// Unique cache key for this request
    /// </summary>
    string CacheKey { get; }
    
    /// <summary>
    /// Absolute cache expiration time
    /// </summary>
    TimeSpan CacheExpiration { get; }
    
    /// <summary>
    /// Sliding expiration (optional)
    /// </summary>
    TimeSpan? SlidingExpiration { get; }
}
