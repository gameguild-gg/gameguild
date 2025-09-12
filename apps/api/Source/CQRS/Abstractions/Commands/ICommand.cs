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
