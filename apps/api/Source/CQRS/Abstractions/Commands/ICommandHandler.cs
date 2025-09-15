namespace GameGuild.CQRS;

/// <summary>
/// Defines a handler for a command with a response.
/// Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> where TCommand : ICommand<TResponse> { }

/// <summary>
/// Defines a handler for a command without a response.
/// Commands represent write operations that modify system state.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : ICommand { }

/// <summary>
/// Defines a handler for a command that returns a Result<TValue>.
/// Commands represent write operations that modify system state with enhanced error handling.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
/// <typeparam name="TValue">The type of value wrapped in Result</typeparam>
public interface IResultCommandHandler<in TCommand, TValue> : IRequestHandler<TCommand, Result<TValue>>
    where TCommand : IResultCommand<TValue>
{ }

/// <summary>
/// Defines a handler for a command that returns a Result.
/// Commands represent write operations that modify system state with enhanced error handling.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled</typeparam>
public interface IResultCommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : IResultCommand
{ }
