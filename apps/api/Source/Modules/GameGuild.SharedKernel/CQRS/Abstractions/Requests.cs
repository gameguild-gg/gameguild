namespace GameGuild.CQRS;

/// <summary>
///     Base request interface for all command/query requests
/// </summary>
public interface IRequestBase { }

/// <summary>
///     Marker interface to represent a request with a response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse> : IRequestBase { }

/// <summary>
///     Marker interface to represent a request without a response
/// </summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>
///     Marker interface for commands that return a result.
///     Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
///     Marker interface for commands without a response.
///     Commands represent write operations that modify system state.
/// </summary>
public interface ICommand : IRequest { }

/// <summary>
///     Marker interface for queries in the system.
///     Queries are read-only operations that return data without modifying state.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

/// <summary>
///     Base class for paginated queries.
///     Exposes Skip/Take offset parameters; return type is the unified <see cref="PagedResult{T}" />.
/// </summary>
public abstract class PaginatedQuery<TResponse> : IQuery<PagedResult<TResponse>>
{
    /// <summary>
    ///     Number of items to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    ///     Number of items to take (page size)
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    ///     Search term for filtering
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    ///     Include soft-deleted entities
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
///     Request handler delegate
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
/// <returns>Response</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
///     Service factory delegate
/// </summary>
public delegate object? ServiceFactory(Type serviceType);
