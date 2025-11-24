using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Handles getting all resource quotas for a tenant
/// </summary>
public class GetTenantResourceQuotasQueryHandler(IResourceQuotaRepository resourceQuotaRepository) : IQueryHandler<GetTenantResourceQuotasQuery, IEnumerable<ResourceQuotaResponse>>
{
    public async Task<IEnumerable<ResourceQuotaResponse>> Handle(GetTenantResourceQuotasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quotas = await resourceQuotaRepository.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return quotas.Select(quota => new ResourceQuotaResponse
            {
                Id = quota.Id,
                TenantId = quota.TenantId ?? Guid.Empty,
                Type = quota.Type,
                Limit = quota.HardLimit ?? quota.SoftLimit ?? 0,
                CurrentUsage = quota.CurrentUsage,
                RemainingQuota = Math.Max(0, (quota.HardLimit ?? quota.SoftLimit ?? 0) - quota.CurrentUsage),
                UsagePercentage = (decimal) quota.GetUsagePercentage(),
                SoftLimitPercentage = quota.SoftLimit.HasValue && quota.HardLimit.HasValue ? (decimal) quota.SoftLimit.Value / quota.HardLimit.Value * 100 : 0,
                IsActive = quota.IsActive,
                Period = quota.Period,
                LastResetDate = quota.LastReset ?? DateTime.MinValue,
                NextResetDate = quota.GetNextResetTime() ?? DateTime.MaxValue,
                Description = null,
                IsSoftLimitExceeded = quota.IsSoftLimitExceeded(),
                IsHardLimitExceeded = quota.IsHardLimitExceeded(),
                ShouldReset = quota.ShouldReset()
            }
        );
    }
}
