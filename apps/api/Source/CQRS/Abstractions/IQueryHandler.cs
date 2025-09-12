namespace GameGuild.CQRS;

/// <summary>
///  Defines a handler for a query.
///  Queries represent read-only operations that return data without modifying state.
/// </summary>
/// <typeparam name="TQuery">The type of query being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
