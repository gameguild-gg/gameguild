namespace GameGuild.Modules.TestingLab;

/// <summary> Base interface for command handlers in the Testing Lab module </summary>
/// <typeparam name="TCommand"> The command type </typeparam>
/// <typeparam name="TResult"> The result type </typeparam>
public interface ITestingLabCommandHandler<in TCommand, TResult> : GameGuild.CQRS.IRequestHandler<TCommand, TResult>
  where TCommand : GameGuild.CQRS.IRequest<TResult>
{ }

/// <summary> Interface for command handlers that don't return a result </summary>
/// <typeparam name="TCommand"> The command type </typeparam>
public interface ITestingLabCommandHandler<in TCommand> : GameGuild.CQRS.IRequestHandler<TCommand>
  where TCommand : GameGuild.CQRS.IRequest
{ }
