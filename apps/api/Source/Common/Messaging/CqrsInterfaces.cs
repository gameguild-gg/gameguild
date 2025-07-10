using GameGuild.Common.Models;
using MediatR;

namespace GameGuild.Common;

/// <summary>
/// Base interface for all commands in the system
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Base interface for commands that return a result
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for all queries in the system
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Interface for command handlers
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

/// <summary>
/// Interface for command handlers that return a result
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Interface for query handlers
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Base class for paginated queries
/// </summary>
public abstract class PaginatedQuery<TResponse> : IQuery<PagedResult<TResponse>>
{
    /// <summary>
    /// Number of items to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of items to take (page size)
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Search term for filtering
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Include soft-deleted entities
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
