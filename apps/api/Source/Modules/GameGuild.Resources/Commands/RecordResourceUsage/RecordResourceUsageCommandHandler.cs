using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for recording resource usage and updating quotas
/// </summary>
public class RecordResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository, IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<RecordResourceUsageCommand, Guid>
{
    public async Task<Guid> Handle(RecordResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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
        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType, cancellationToken).ConfigureAwait(false);

        if (quota == null) return usageRecord.Id;

        // Check if quota needs reset before adding usage
        if (quota.ShouldReset()) { quota.ResetUsage(); }

        // Add usage to quota
        quota.AddUsage(request.Count);
        quota.UpdatedAt = DateTime.UtcNow;

        await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);

        return usageRecord.Id;
    }
}
