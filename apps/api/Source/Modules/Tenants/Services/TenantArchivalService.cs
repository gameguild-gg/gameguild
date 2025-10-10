using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Tenants.Repositories;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Services;

public interface ITenantArchivalService
{
    Task<TenantArchivalPolicy> CreatePolicyAsync(Guid tenantId, string policyName, int inactivityThresholdDays, int warningDaysBeforeArchival, int autoPurgeAfterDays, string[] notificationEmails);

    Task<TenantArchivalPolicy> UpdatePolicyAsync(Guid policyId, bool isEnabled, int? inactivityThresholdDays, int? warningDaysBeforeArchival, int? autoPurgeAfterDays);

    Task<TenantArchiveRecord> ArchiveTenantAsync(Guid tenantId, Guid archivedBy, TenantArchivalReason reason);

    Task<TenantArchiveRecord> RestoreTenantAsync(Guid archiveRecordId, Guid restoredBy);

    Task PurgeTenantAsync(Guid archiveRecordId);

    Task<List<Guid>> DetectInactiveTenantsAsync(CancellationToken cancellationToken = default);

    Task SendArchivalWarningAsync(Guid tenantId);
}

public class TenantArchivalService : ITenantArchivalService
{
    private readonly ITenantArchivalRepository _repository;
    private readonly ILogger<TenantArchivalService> _logger;

    public TenantArchivalService(ITenantArchivalRepository repository, ILogger<TenantArchivalService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TenantArchivalPolicy> CreatePolicyAsync(Guid tenantId, string policyName, int inactivityThresholdDays, int warningDaysBeforeArchival, int autoPurgeAfterDays, string[] notificationEmails)
    {
        var policy = new TenantArchivalPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyName = policyName,
            IsEnabled = true,
            InactivityThresholdDays = inactivityThresholdDays,
            WarningDaysBeforeArchival = warningDaysBeforeArchival,
            AutoPurgeAfterDays = autoPurgeAfterDays,
            NotificationEmails = notificationEmails,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreatePolicyAsync(policy);

        _logger.LogInformation("Created archival policy {PolicyId} for tenant {TenantId}", policy.Id, tenantId);

        return policy;
    }

    public async Task<TenantArchivalPolicy> UpdatePolicyAsync(Guid policyId, bool isEnabled, int? inactivityThresholdDays, int? warningDaysBeforeArchival, int? autoPurgeAfterDays)
    {
        var policy = await _repository.GetPolicyByIdAsync(policyId);
        if (policy == null)
        {
            throw new InvalidOperationException($"Archival policy {policyId} not found");
        }

        policy.IsEnabled = isEnabled;
        if (inactivityThresholdDays.HasValue) policy.InactivityThresholdDays = inactivityThresholdDays.Value;
        if (warningDaysBeforeArchival.HasValue) policy.WarningDaysBeforeArchival = warningDaysBeforeArchival.Value;
        if (autoPurgeAfterDays.HasValue) policy.AutoPurgeAfterDays = autoPurgeAfterDays.Value;
        policy.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdatePolicyAsync(policy);

        _logger.LogInformation("Updated archival policy {PolicyId}", policyId);

        return policy;
    }

    public async Task<TenantArchiveRecord> ArchiveTenantAsync(Guid tenantId, Guid archivedBy, TenantArchivalReason reason)
    {
        var record = new TenantArchiveRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ArchivedBy = archivedBy,
            ArchivedAt = DateTime.UtcNow,
            Reason = reason,
            Status = TenantArchivalStatus.InProgress,
            ArchiveLocation = $"archive/{tenantId}",
            Metadata = "{}"
        };

        record.MarkAsArchived();
        await _repository.CreateArchiveRecordAsync(record);

        _logger.LogInformation("Archived tenant {TenantId} with reason {Reason}", tenantId, reason);

        return record;
    }

    public async Task<TenantArchiveRecord> RestoreTenantAsync(Guid archiveRecordId, Guid restoredBy)
    {
        var record = await _repository.GetArchiveRecordByIdAsync(archiveRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Archive record {archiveRecordId} not found");
        }

        record.MarkAsRestored(restoredBy);
        await _repository.UpdateArchiveRecordAsync(record);

        _logger.LogInformation("Restored tenant {TenantId} from archive", record.TenantId);

        return record;
    }

    public async Task PurgeTenantAsync(Guid archiveRecordId)
    {
        var record = await _repository.GetArchiveRecordByIdAsync(archiveRecordId);
        if (record == null)
        {
            throw new InvalidOperationException($"Archive record {archiveRecordId} not found");
        }

        record.MarkAsPurged();
        await _repository.UpdateArchiveRecordAsync(record);

        _logger.LogInformation("Purged tenant {TenantId} from archive", record.TenantId);
    }

    public async Task<List<Guid>> DetectInactiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _repository.GetActivePoliciesAsync(cancellationToken);
        var inactiveTenants = new List<Guid>();

        foreach (var policy in policies)
        {
            var lastActivity = DateTime.UtcNow.AddDays(-90); // Placeholder - would get actual last activity
            if (policy.ShouldArchive(lastActivity))
            {
                inactiveTenants.Add(policy.TenantId);
            }
        }

        return inactiveTenants;
    }

    public async Task SendArchivalWarningAsync(Guid tenantId)
    {
        var policy = await _repository.GetPolicyByTenantIdAsync(tenantId);
        if (policy == null)
        {
            throw new InvalidOperationException($"No archival policy found for tenant {tenantId}");
        }

        policy.SendWarning();
        await _repository.UpdatePolicyAsync(policy);

        _logger.LogInformation("Sent archival warning to tenant {TenantId}", tenantId);
    }
}
