using GameGuild.Common.Domain;


namespace GameGuild.Common.Abstractions.Messaging;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Domain.Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
