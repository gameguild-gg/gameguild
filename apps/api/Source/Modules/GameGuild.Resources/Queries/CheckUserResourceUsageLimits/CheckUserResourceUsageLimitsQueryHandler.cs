using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for checking user resource usage limits
/// </summary>
public sealed class CheckUserResourceUsageLimitsQueryHandler(IResourceQuotaRepository quotaRepository) : IQueryHandler<CheckUserResourceUsageLimitsQuery, Dictionary<ResourceUsageType, bool>>
{
    public async Task<Dictionary<ResourceUsageType, bool>> Handle(CheckUserResourceUsageLimitsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new Dictionary<ResourceUsageType, bool>();

        if (request.ResourceUsageType.HasValue)
        {
            var quota = await quotaRepository.GetByUserAndTypeAsync(request.UserId, request.ResourceUsageType.Value, cancellationToken).ConfigureAwait(false);

            result[request.ResourceUsageType.Value] = quota?.IsHardLimitExceeded() ?? false;
        }
        else
        {
            var quotas = await quotaRepository.GetByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);

            foreach (var quota in quotas) { result[quota.Type] = quota.IsHardLimitExceeded(); }
        }

        return result;
    }
}
