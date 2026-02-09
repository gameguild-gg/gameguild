using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for toggling user resource quota
/// </summary>
public sealed class ToggleUserResourceQuotaCommandHandler(IResourceQuotaRepository quotaRepository) : ICommandHandler<ToggleUserResourceQuotaCommand>
{
    public async Task<Unit> Handle(ToggleUserResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await quotaRepository.GetByUserAndTypeAsync(request.UserId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota != null)
        {
            quota.IsActive = request.IsActive;
            quota.Touch();
            await quotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
