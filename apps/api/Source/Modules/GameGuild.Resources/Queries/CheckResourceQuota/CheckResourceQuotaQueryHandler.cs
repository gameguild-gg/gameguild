using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Handles checking resource quota enforcement
/// </summary>
public class CheckResourceQuotaQueryHandler(IResourceQuotaRepository resourceQuotaRepository) : IQueryHandler<CheckResourceQuotaQuery, ResourceQuotaEnforcementResult>
{
    public async Task<ResourceQuotaEnforcementResult> Handle(CheckResourceQuotaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);

        // If no quota exists or quota is inactive, allow the request
        if (quota is not { IsActive: true })
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

        // Check if quota needs reset
        if (quota.ShouldReset())
        {
            quota.ResetUsage();
            await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        var projectedUsage = quota.CurrentUsage + request.Amount;
        var isHardLimitExceeded = quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value;
        var isSoftLimitExceeded = quota.SoftLimit.HasValue && projectedUsage > quota.SoftLimit.Value;

        // Hard limit prevents the request
        var isAllowed = !isHardLimitExceeded;

        var result = new ResourceQuotaEnforcementResult
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
            NextReset = quota.GetNextResetTime()
        };

        // Set appropriate message
        if (isHardLimitExceeded) { result.Message = $"Hard limit exceeded. Current: {quota.CurrentUsage}, Limit: {quota.HardLimit}, Requested: {request.Amount}"; }
        else if (isSoftLimitExceeded) { result.Message = $"Soft limit exceeded. Current: {quota.CurrentUsage}, Limit: {quota.SoftLimit}, Requested: {request.Amount}"; }
        else { result.Message = $"Within quota limits. Current: {quota.CurrentUsage}, Available: {(quota.HardLimit ?? int.MaxValue) - quota.CurrentUsage}"; }

        return result;
    }
}
