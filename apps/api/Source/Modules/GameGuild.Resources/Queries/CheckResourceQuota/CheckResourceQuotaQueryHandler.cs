using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handles checking resource quota enforcement.
///     <para>
///     <b>ADVISORY ONLY:</b> This is a read-only query that does NOT mutate state or reserve quota.
///     The result tells callers whether the operation WOULD be allowed, but does not guarantee
///     the quota will still be available when the actual operation executes.
///     </para>
///     <para>
///     For authoritative enforcement, commands should use <c>[RequiresQuota]</c> attribute
///     which uses <c>TryAtomicConsumeAsync</c> for atomic reservation.
///     </para>
/// </summary>
public sealed class CheckResourceQuotaQueryHandler(IResourceQuotaRepository resourceQuotaRepository) : IQueryHandler<CheckResourceQuotaQuery, ResourceQuotaEnforcementResult>
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

        // Calculate effective current usage (0 if reset is due, otherwise current)
        // NOTE: Do NOT mutate state during read operations - reset happens during actual usage recording
        var effectiveCurrentUsage = quota.ShouldReset() ? 0 : quota.CurrentUsage;

        var projectedUsage = effectiveCurrentUsage + request.Amount;
        var isHardLimitExceeded = quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value;
        var isSoftLimitExceeded = quota.SoftLimit.HasValue && projectedUsage > quota.SoftLimit.Value;

        // Hard limit prevents the request
        var isAllowed = !isHardLimitExceeded;

        // Calculate usage percentage based on effective usage
        var usagePercentage = quota.HardLimit.HasValue && quota.HardLimit.Value > 0
            ? (double)effectiveCurrentUsage / quota.HardLimit.Value * 100
            : 0;

        var result = new ResourceQuotaEnforcementResult
        {
            IsAllowed = isAllowed,
            IsSoftLimitExceeded = isSoftLimitExceeded,
            IsHardLimitExceeded = isHardLimitExceeded,
            CurrentUsage = effectiveCurrentUsage,
            SoftLimit = quota.SoftLimit,
            HardLimit = quota.HardLimit,
            UsagePercentage = usagePercentage,
            ExcessAmount = isHardLimitExceeded ? projectedUsage - quota.HardLimit!.Value : 0,
            Type = request.Type,
            NextReset = quota.GetNextResetTime()
        };

        // Set appropriate message
        if (isHardLimitExceeded) { result.Message = $"Hard limit exceeded. Current: {effectiveCurrentUsage}, Limit: {quota.HardLimit}, Requested: {request.Amount}"; }
        else if (isSoftLimitExceeded) { result.Message = $"Soft limit exceeded. Current: {effectiveCurrentUsage}, Limit: {quota.SoftLimit}, Requested: {request.Amount}"; }
        else { result.Message = $"Within quota limits. Current: {effectiveCurrentUsage}, Available: {(quota.HardLimit ?? int.MaxValue) - effectiveCurrentUsage}"; }

        return result;
    }
}
