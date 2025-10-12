namespace GameGuild.CQRS;

/// <summary>
///   Defines a handler for a command that returns a Result<TValue> . Commands represent write operations that modify system state with enhanced error handling.
/// </summary>
/// <typeparam name="TCommand"> The type of command being handled </typeparam>
/// <typeparam name="TValue"> The type of value wrapped in Result </typeparam>
public interface IResultCommandHandler<in TCommand, TValue> : IRequestHandler<TCommand, Result<TValue>> where TCommand : IResultCommand<TValue> { }
