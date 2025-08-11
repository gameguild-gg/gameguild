using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Interface for command handlers that return a result
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
  where TCommand : ICommand<TResponse> { }

/// <summary>
/// Interface for command handlers
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
  where TCommand : ICommand { }
