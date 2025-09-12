namespace GameGuild.CQRS;

/// <summary>
/// Pipeline behavior for caching responses
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest, ICacheableRequest
{
    private readonly ICacheService _cacheService;

    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the CachingBehavior class
    /// </summary>
    /// <param name="cacheService">Cache service</param>
    /// <param name="logger">Logger</param>
    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the pipeline behavior
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var cacheKey = request.CacheKey;

        // Try to get from cache first
        var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cachedResponse is not null && !EqualityComparer<TResponse>.Default.Equals(cachedResponse, default))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);

            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        // Execute the request
        var response = await next().ConfigureAwait(false);

        // Cache the response
        await _cacheService.SetAsync(cacheKey, response, request.CacheExpiration, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
