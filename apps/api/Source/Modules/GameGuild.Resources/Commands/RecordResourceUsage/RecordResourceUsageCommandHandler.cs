using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for recording resource usage and updating quotas.
///     This handler enforces hard limits - if recording would exceed the quota, it throws QuotaExceededException.
///     When SkipQuotaIncrement is true (quota was already atomically consumed), only creates the audit record.
/// </summary>
public sealed class RecordResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository, IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<RecordResourceUsageCommand, Guid>
{
    public async Task<Guid> Handle(RecordResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ResourceQuota? quota = null;

        // Only check and update quota if not skipping (quota wasn't already atomically consumed)
        if (!request.SkipQuotaIncrement)
        {
            // First, check if this recording would exceed the quota (FAIL-CLOSED)
            quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType, cancellationToken).ConfigureAwait(false);

            if (quota != null && quota.IsActive)
            {
                // Check if quota needs reset
                if (quota.ShouldReset()) { quota.ResetUsage(); }

                // Validate against hard limit BEFORE recording
                if (quota.HardLimit.HasValue)
                {
                    var projectedUsage = quota.CurrentUsage + request.Count;
                    if (projectedUsage > quota.HardLimit.Value)
                    {
                        throw new QuotaExceededException(
                            $"Cannot record {request.Count} units of {request.ResourceUsageType}. Would exceed hard limit.",
                            request.ResourceUsageType,
                            quota.CurrentUsage,
                            quota.HardLimit.Value,
                            request.TenantId);
                    }
                }
            }
        }

        // Create usage record using factory method (avoids reflection, type-safe)
        var usageRecord = UsageRecord.CreateDaily(
            request.ResourceUsageType,
            request.TenantId,
            request.Count,
            request.PeriodStart,
            userId: null,
            source: request.Source);

        // Override period end if specified
        if (request.PeriodEnd != default)
        {
            usageRecord.PeriodEnd = request.PeriodEnd;
        }

        // Set metadata if provided
        if (!string.IsNullOrEmpty(request.Metadata))
        {
            usageRecord.Metadata = request.Metadata;
        }

        // Link to quota if exists
        if (quota != null)
        {
            usageRecord.ResourceQuotaId = quota.Id;
        }

        await usageRecordRepository.CreateAsync(usageRecord, cancellationToken).ConfigureAwait(false);

        // Update quota if exists and not skipping
        if (!request.SkipQuotaIncrement && quota != null)
        {
            quota.AddUsage(request.Count);
            quota.Touch();
            await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return usageRecord.Id;
    }
}
