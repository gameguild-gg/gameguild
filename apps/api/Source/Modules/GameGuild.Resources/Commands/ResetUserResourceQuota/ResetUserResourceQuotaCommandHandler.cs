using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for resetting user resource quota
/// </summary>
public sealed class ResetUserResourceQuotaCommandHandler(IResourceQuotaRepository quotaRepository) : ICommandHandler<ResetUserResourceQuotaCommand>
{
    public async Task<Unit> Handle(ResetUserResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await quotaRepository.GetByUserAndTypeAsync(request.UserId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota != null)
        {
            quota.Reset();
            await quotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
