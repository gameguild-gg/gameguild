using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Base interface for commands that return a result
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Base interface for all commands in the system
/// </summary>
public interface ICommand : IRequest { }
