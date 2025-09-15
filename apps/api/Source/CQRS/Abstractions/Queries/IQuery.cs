namespace GameGuild.CQRS;

/// <summary>
/// Marker interface for queries in the system.
/// Queries are read-only operations that return data without modifying state.
/// This is a marker interface to provide semantic meaning and type safety for CQRS pattern.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern
}

/// <summary>
/// Marker interface for queries that return a Result<TValue> for enhanced error handling.
/// Queries are read-only operations that return data without modifying state.
/// </summary>
/// <typeparam name="TValue">The value type wrapped in Result</typeparam>
public interface IResultQuery<TValue> : IQuery<Result<TValue>>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern with Result<T> return type
}
