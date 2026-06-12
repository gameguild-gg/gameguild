using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     In-process enforcement sink used by the API host when no external gateway adapter is configured.
/// </summary>
public sealed class LocalResourceThrottlingEnforcementSink(ILogger<LocalResourceThrottlingEnforcementSink> logger) : IResourceThrottlingEnforcementSink
{
    public Task<ThrottlingEnforcementResult> ApplyAsync(
        Guid tenantId,
        ResourceUsageType type,
        long requestedAmount,
        ThrottlingResult decision,
        CancellationToken cancellationToken = default)
    {
        var enforcedAt = SystemClock.UtcNow;
        var isEnforced = !decision.IsAllowed || decision.DelayMs > 0;
        var reference = $"local-throttle:{tenantId:N}:{type}:{enforcedAt:yyyyMMddHHmmssfff}";

        logger.LogInformation(
            "Applied resource throttle decision {Reference} for tenant {TenantId}, type {Type}, amount {Amount}, allowed {Allowed}, delay {DelayMs}ms",
            reference,
            tenantId,
            type,
            requestedAmount,
            decision.IsAllowed,
            decision.DelayMs);

        return Task.FromResult(new ThrottlingEnforcementResult(
            tenantId,
            type,
            isEnforced,
            reference,
            enforcedAt,
            decision.DelayMs,
            decision.Reason));
    }
}
