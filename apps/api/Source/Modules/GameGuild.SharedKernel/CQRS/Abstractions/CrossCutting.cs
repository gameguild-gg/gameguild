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

    /// <summary>
    ///     Create a stream via a single stream handler
    /// </summary>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStream<TResponse> request, CancellationToken cancellationToken = default);
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
///     Marker interface for cacheable requests
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    ///     Gets the cache key for this request
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    ///     Gets the cache expiration time
    /// </summary>
    TimeSpan CacheExpiration { get; }
}

/// <summary>
///     Cache service interface
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
///     Simple validator interface. 
///     <b>Deprecated:</b> Use <c>FluentValidation.IValidator&lt;T&gt;</c> instead.
///     The <see cref="ValidationBehavior{TRequest,TResponse}"/> now uses FluentValidation directly.
/// </summary>
/// <typeparam name="T">Type to validate</typeparam>
[Obsolete("Use FluentValidation.IValidator<T> instead. This interface is no longer consumed by the CQRS pipeline.")]
public interface IValidator<T>
{
    /// <summary>
    ///     Validates the instance
    /// </summary>
    Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellationToken = default);
}
