using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for recording resource usage and updating quotas.
///     This handler enforces hard limits - if recording would exceed the quota, it throws QuotaExceededException.
/// </summary>
public class RecordResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository, IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<RecordResourceUsageCommand, Guid>
{
    public async Task<Guid> Handle(RecordResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // First, check if this recording would exceed the quota (FAIL-CLOSED)
        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType, cancellationToken).ConfigureAwait(false);

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

        // Create usage record
        var usageRecord = new UsageRecord
        {
            Id = Guid.NewGuid(), Type = request.ResourceUsageType, Count = request.Count, PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, Metadata = request.Metadata, CreatedAt = DateTime.UtcNow
        };

        // Set TenantId using reflection since the setter is protected
        var tenantIdProperty = typeof(UsageRecord).GetProperty("TenantId");
        tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(usageRecord, new object[] { request.TenantId });

        await usageRecordRepository.CreateAsync(usageRecord, cancellationToken).ConfigureAwait(false);

        // Update quota if exists
        if (quota != null)
        {
            quota.AddUsage(request.Count);
            quota.UpdatedAt = DateTime.UtcNow;
            await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return usageRecord.Id;
    }
}
