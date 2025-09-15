namespace GameGuild.CQRS;

/// <summary>
/// Marker interface for commands that return a result.
/// Commands represent write operations that modify system state.
/// This is a marker interface to provide semantic meaning and type safety.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern
}

/// <summary>
/// Marker interface for commands without a response.
/// Commands represent write operations that modify system state.
/// This is a marker interface to provide semantic meaning and type safety.
/// </summary>
public interface ICommand : IRequest
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern
}

/// <summary>
/// Marker interface for commands that return a Result<TValue> for enhanced error handling.
/// Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TValue">The value type wrapped in Result</typeparam>
public interface IResultCommand<TValue> : ICommand<Result<TValue>>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern with Result<T> return type
}

/// <summary>
/// Marker interface for commands that return a Result for enhanced error handling.
/// Commands represent write operations that modify system state.
/// </summary>
public interface IResultCommand : ICommand<Result>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern with Result return type
}
