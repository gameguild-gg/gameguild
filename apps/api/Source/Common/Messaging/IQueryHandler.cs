namespace GameGuild.Common.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Domain.Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
