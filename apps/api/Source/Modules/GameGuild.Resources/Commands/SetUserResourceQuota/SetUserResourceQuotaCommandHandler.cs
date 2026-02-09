using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for setting user resource quota
/// </summary>
public sealed class SetUserResourceQuotaCommandHandler(IResourceQuotaRepository quotaRepository) : ICommandHandler<SetUserResourceQuotaCommand>
{
    public async Task<Unit> Handle(SetUserResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await quotaRepository.GetByUserAndTypeAsync(request.UserId, request.Type, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            existing.SoftLimit = request.SoftLimit;
            existing.HardLimit = request.HardLimit;
            existing.Period = request.Period;
            existing.IsActive = request.IsActive;
            existing.ResetTime = request.ResetTime;
            existing.Touch();

            await quotaRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var quota = new ResourceQuota
            {
                UserId = request.UserId,
                Type = request.Type,
                SoftLimit = request.SoftLimit,
                HardLimit = request.HardLimit,
                Period = request.Period,
                IsActive = request.IsActive,
                ResetTime = request.ResetTime
            };

            await quotaRepository.CreateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
