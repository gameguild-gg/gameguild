namespace GameGuild.CQRS;

/// <summary>
///   Marker interface for commands that return a Result<TValue> for enhanced error handling. Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TValue"> The value type wrapped in Result </typeparam>
public interface IResultCommand<TValue> : ICommand<Result<TValue>>
{
    // Marker interface - no additional members needed
    // Provides semantic meaning for CQRS pattern with Result<T> return type
}
