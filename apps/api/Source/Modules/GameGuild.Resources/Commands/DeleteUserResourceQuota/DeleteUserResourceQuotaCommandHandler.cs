using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for deleting user resource quota
/// </summary>
public sealed class DeleteUserResourceQuotaCommandHandler(IResourceQuotaRepository quotaRepository) : ICommandHandler<DeleteUserResourceQuotaCommand>
{
    public async Task<Unit> Handle(DeleteUserResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await quotaRepository.DeleteByUserAndTypeAsync(request.UserId, request.Type, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
