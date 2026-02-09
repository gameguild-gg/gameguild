using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for getting user resource quotas
/// </summary>
public sealed class GetUserResourceQuotasQueryHandler(IResourceQuotaRepository quotaRepository) : IQueryHandler<GetUserResourceQuotasQuery, IEnumerable<ResourceQuotaResponse>>
{
    public async Task<IEnumerable<ResourceQuotaResponse>> Handle(GetUserResourceQuotasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quotas = await quotaRepository.GetByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        return quotas.Select(q => new ResourceQuotaResponse
        {
            Id = q.Id,
            TenantId = q.TenantId ?? Guid.Empty,
            Type = q.Type,
            Limit = q.HardLimit ?? q.SoftLimit ?? 0,
            SoftLimit = q.SoftLimit,
            HardLimit = q.HardLimit,
            CurrentUsage = q.CurrentUsage,
            RemainingQuota = Math.Max(0, (q.HardLimit ?? q.SoftLimit ?? 0) - q.CurrentUsage),
            UsagePercentage = (decimal)q.GetUsagePercentage(),
            SoftLimitPercentage = q.SoftLimit.HasValue && q.HardLimit.HasValue ? (decimal)q.SoftLimit.Value / q.HardLimit.Value * 100 : 0,
            IsActive = q.IsActive,
            Period = q.Period,
            LastResetDate = q.LastReset ?? DateTime.MinValue,
            NextResetDate = q.GetNextResetTime() ?? DateTime.MaxValue,
            Description = null,
            IsSoftLimitExceeded = q.IsSoftLimitExceeded(),
            IsHardLimitExceeded = q.IsHardLimitExceeded(),
            ShouldReset = q.ShouldReset()
        });
    }
}
