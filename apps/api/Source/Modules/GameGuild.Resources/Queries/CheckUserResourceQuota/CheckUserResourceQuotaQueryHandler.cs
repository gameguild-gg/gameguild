using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for checking user resource quota
/// </summary>
public sealed class CheckUserResourceQuotaQueryHandler(IResourceQuotaRepository quotaRepository) : IQueryHandler<CheckUserResourceQuotaQuery, ResourceQuotaEnforcementResult>
{
    public async Task<ResourceQuotaEnforcementResult> Handle(CheckUserResourceQuotaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await quotaRepository.GetByUserAndTypeAsync(request.UserId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota == null || !quota.IsActive)
        {
            return new ResourceQuotaEnforcementResult
            {
                IsAllowed = true,
                IsSoftLimitExceeded = false,
                IsHardLimitExceeded = false,
                CurrentUsage = 0,
                SoftLimit = null,
                HardLimit = null,
                UsagePercentage = 0,
                ExcessAmount = 0,
                Message = "No active quota limit",
                Type = request.Type,
                NextReset = null
            };
        }

        var projectedUsage = quota.CurrentUsage + request.Amount;
        var isHardLimitExceeded = quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value;
        var isSoftLimitExceeded = quota.SoftLimit.HasValue && projectedUsage > quota.SoftLimit.Value;
        var isAllowed = !isHardLimitExceeded;

        return new ResourceQuotaEnforcementResult
        {
            IsAllowed = isAllowed,
            IsSoftLimitExceeded = isSoftLimitExceeded,
            IsHardLimitExceeded = isHardLimitExceeded,
            CurrentUsage = quota.CurrentUsage,
            SoftLimit = quota.SoftLimit,
            HardLimit = quota.HardLimit,
            UsagePercentage = quota.GetUsagePercentage(),
            ExcessAmount = isHardLimitExceeded ? projectedUsage - quota.HardLimit!.Value : 0,
            Type = request.Type,
            NextReset = quota.GetNextResetTime(),
            Message = isHardLimitExceeded 
                ? $"Usage would exceed hard limit of {quota.HardLimit!.Value}" 
                : isSoftLimitExceeded 
                    ? $"Usage exceeds soft limit of {quota.SoftLimit!.Value}" 
                    : null
        };
    }
}
