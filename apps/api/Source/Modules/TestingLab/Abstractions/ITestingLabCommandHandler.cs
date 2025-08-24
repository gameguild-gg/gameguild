using MediatR;

namespace GameGuild.Modules.TestingLab.Abstractions;

/// <summary>
/// Base interface for command handlers in the Testing Lab module
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public interface ITestingLabCommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : IRequest<TResult>
{
}

/// <summary>
/// Interface for command handlers that don't return a result
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface ITestingLabCommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : IRequest
{
}
