namespace GameGuild.CQRS;

/// <summary>
///     Send a request through the mediator pipeline to be handled by a single handler.
/// </summary>
public interface ISender
{
    /// <summary>
    ///     Asynchronously send a request to a single handler
    /// </summary>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously send a request to a single handler without expecting a response
    /// </summary>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    ///     Asynchronously send an object request to a single handler via dynamic dispatch
    /// </summary>
    Task<object?> Send(object request, CancellationToken cancellationToken = default);

}

/// <summary>
///     Defines a mediator to encapsulate request/response and publishing interaction patterns
/// </summary>
public interface IMediator : ISender, IPublisher { }

/// <summary>
///     Marker interface for tenant-scoped requests
/// </summary>
public interface ITenantScoped
{
    Guid? TenantId { get; }
}

/// <summary>
///     Cache service interface for request-level caching.
///     Default implementation: <see cref="Implementation.MemoryCacheService"/>.
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     Gets a value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets a value in cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a value from cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
///     Optional cache capability for removing app cache entries by wildcard pattern.
/// </summary>
public interface IPatternCacheService : ICacheService
{
    /// <summary>
    ///     Removes app cache entries matching a wildcard pattern and returns the number of keys requested for removal.
    /// </summary>
    Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
