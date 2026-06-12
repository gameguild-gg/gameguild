using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Implementation of resource throttling policy management
/// </summary>
public class ResourceThrottlingService(
    IResourceThrottlingPolicyRepository policyRepository,
    IResourceQuotaRepository quotaRepository,
    ILogger<ResourceThrottlingService> logger,
    IResourceThrottlingEnforcementSink? enforcementSink = null) : IResourceThrottlingService
{
    public const string MeterName = "GameGuild.Resources";

    private static readonly Meter ThrottlingMeter = new(MeterName);

    private static readonly Counter<long> ThrottlingDecisionCounter = ThrottlingMeter.CreateCounter<long>(
        "gameguild.resources.throttling.decisions",
        unit: "decisions",
        description: "Resource throttling decisions evaluated by policy");

    private static readonly Counter<long> ThrottlingBlockedCounter = ThrottlingMeter.CreateCounter<long>(
        "gameguild.resources.throttling.blocked",
        unit: "requests",
        description: "Resource requests blocked by throttling policy");

    private static readonly Histogram<double> ThrottlingDelayHistogram = ThrottlingMeter.CreateHistogram<double>(
        "gameguild.resources.throttling.delay",
        unit: "ms",
        description: "Delay applied to resource requests by throttling policy");

    private static readonly Histogram<double> ThrottlingUsageHistogram = ThrottlingMeter.CreateHistogram<double>(
        "gameguild.resources.throttling.usage",
        unit: "%",
        description: "Resource usage percentage observed during throttling decisions");

    public async Task<ResourceThrottlingPolicy> SetPolicyAsync(
        Guid tenantId,
        ResourceUsageType type,
        ThrottlingStrategy strategy,
        long threshold,
        string? configuration = null,
        CancellationToken cancellationToken = default
    )
    {
        var existingPolicy = await policyRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        if (existingPolicy != null)
        {
            existingPolicy.Strategy = strategy;
            existingPolicy.ThrottlingThresholdPercent = (int) threshold;
            existingPolicy.Configuration = configuration;
            existingPolicy.Touch();
            existingPolicy = await policyRepository.UpdateAsync(existingPolicy, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existingPolicy = new ResourceThrottlingPolicy { ResourceType = type, Strategy = strategy, ThrottlingThresholdPercent = (int) threshold, Configuration = configuration, IsActive = true };
            existingPolicy.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });
            existingPolicy = await policyRepository.CreateAsync(existingPolicy, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Set throttling policy for tenant {TenantId}, type {Type}: Strategy={Strategy}, Threshold={Threshold}", tenantId, type, strategy, threshold);

        return existingPolicy;
    }

    public async Task<ResourceThrottlingPolicy?> GetPolicyAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await policyRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ResourceThrottlingPolicy>> GetTenantPoliciesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await policyRepository.GetByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeletePolicyAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var policy = await policyRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        if (policy == null) return false;

        var deleted = await policyRepository.DeleteAsync(policy.Id, cancellationToken).ConfigureAwait(false);

        if (deleted) { logger.LogInformation("Deleted throttling policy for tenant {TenantId}, type {Type}", tenantId, type); }

        return deleted;
    }

    public async Task<(bool ShouldBlock, int DelayMs)> ShouldThrottleAsync(Guid tenantId, ResourceUsageType type, long currentUsage, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        if (policy is not { IsActive: true })
        {
            RecordThrottlingMetrics(type, ThrottlingStrategy.None, isAllowed: true, delayMs: 0, currentUsage);
            return (false, 0);
        }

        var shouldBlock = policy.ShouldBlock(currentUsage);
        var delayMs = policy.CalculateDelayMs(currentUsage);
        RecordThrottlingMetrics(type, policy.Strategy, !shouldBlock, delayMs, currentUsage);

        return (shouldBlock, delayMs);
    }

    public async Task<ThrottlingResult> ApplyThrottlingAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(tenantId, type, cancellationToken).ConfigureAwait(false);

        if (policy is not { IsActive: true })
        {
            var inactiveResult = new ThrottlingResult { IsAllowed = true, DelayMs = 0, Reason = "No throttling policy active", AppliedStrategy = ThrottlingStrategy.None };
            RecordThrottlingMetrics(type, inactiveResult.AppliedStrategy, inactiveResult.IsAllowed, inactiveResult.DelayMs, currentUsage: 0);
            return inactiveResult;
        }

        // Get current usage from quota
        var quota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, type, cancellationToken).ConfigureAwait(false);
        var currentUsage = quota?.CurrentUsage ?? 0;

        var shouldBlock = policy.ShouldBlock(currentUsage);
        var delayMs = policy.CalculateDelayMs(currentUsage);

        var result = new ThrottlingResult { IsAllowed = !shouldBlock, DelayMs = delayMs, AppliedStrategy = policy.Strategy };

        if (shouldBlock)
        {
            result.Reason = $"Resource usage ({currentUsage}) exceeds throttling threshold ({policy.ThrottlingThresholdPercent})";

            logger.LogWarning("Throttled request for tenant {TenantId}, type {Type}: {Reason}", tenantId, type, result.Reason);
        }
        else if (delayMs > 0)
        {
            result.Reason = $"Request delayed by {delayMs}ms due to approaching threshold";

            logger.LogDebug("Applied throttling delay for tenant {TenantId}, type {Type}: {DelayMs}ms", tenantId, type, delayMs);
        }
        else { result.Reason = "Within normal operating limits"; }

        RecordThrottlingMetrics(type, result.AppliedStrategy, result.IsAllowed, result.DelayMs, currentUsage);
        await ApplyEnforcementAsync(tenantId, type, requestedAmount, result, cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task<IEnumerable<ResourceThrottlingPolicy>> GetActivePoliciesAsync(ResourceUsageType? type = null, CancellationToken cancellationToken = default)
    {
        return await policyRepository.GetActivePoliciesAsync(type, cancellationToken).ConfigureAwait(false);
    }

    private static void RecordThrottlingMetrics(ResourceUsageType type, ThrottlingStrategy strategy, bool isAllowed, int delayMs, double currentUsage)
    {
        var tags = new TagList
        {
            { "resource.type", type.ToString() },
            { "strategy", strategy.ToString() },
            { "allowed", isAllowed.ToString().ToLowerInvariant() }
        };

        ThrottlingDecisionCounter.Add(1, tags);

        if (!isAllowed)
        {
            ThrottlingBlockedCounter.Add(1, tags);
        }

        if (delayMs is > 0 and < int.MaxValue)
        {
            ThrottlingDelayHistogram.Record(delayMs, tags);
        }

        if (currentUsage > 0)
        {
            ThrottlingUsageHistogram.Record(Math.Round(currentUsage, 2), tags);
        }
    }

    private async Task ApplyEnforcementAsync(Guid tenantId, ResourceUsageType type, long requestedAmount, ThrottlingResult result, CancellationToken cancellationToken)
    {
        if (enforcementSink is null)
        {
            return;
        }

        var enforcement = await enforcementSink.ApplyAsync(tenantId, type, requestedAmount, result, cancellationToken).ConfigureAwait(false);
        result.EnforcementReference = enforcement.EnforcementReference;
        result.EnforcedAt = enforcement.EnforcedAt;
    }
}
