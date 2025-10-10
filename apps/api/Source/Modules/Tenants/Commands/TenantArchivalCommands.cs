using GameGuild.Core.Messaging;
using GameGuild.Modules.Tenants.Entities;

namespace GameGuild.Modules.Tenants.Commands;

// Create Policy Command
public record CreateTenantArchivalPolicyCommand(
    Guid TenantId,
    string PolicyName,
    int InactivityThresholdDays,
    int WarningDaysBeforeArchival,
    int AutoPurgeAfterDays,
    string[] NotificationEmails
) : IRequest<Result<TenantArchivalPolicy>>;

// Update Policy Command
public record UpdateTenantArchivalPolicyCommand(
    Guid PolicyId,
    bool IsEnabled,
    int? InactivityThresholdDays,
    int? WarningDaysBeforeArchival,
    int? AutoPurgeAfterDays
) : IRequest<Result<TenantArchivalPolicy>>;

// Archive Tenant Command
public record ArchiveTenantCommand(
    Guid TenantId,
    Guid ArchivedBy,
    TenantArchivalReason Reason
) : IRequest<Result<TenantArchiveRecord>>;

// Restore Tenant Command
public record RestoreTenantFromArchiveCommand(
    Guid ArchiveRecordId,
    Guid RestoredBy
) : IRequest<Result<TenantArchiveRecord>>;

// Purge Tenant Command
public record PurgeTenantCommand(
    Guid ArchiveRecordId
) : IRequest<Result>;

// Detect Inactive Tenants Query
public record DetectInactiveTenantsQuery() : IRequest<Result<List<Guid>>>;

// Send Warning Command
public record SendTenantArchivalWarningCommand(
    Guid TenantId
) : IRequest<Result>;

// Get Policy Query
public record GetTenantArchivalPolicyQuery(
    Guid TenantId
) : IRequest<Result<TenantArchivalPolicy>>;

// Get Archive Record Query
public record GetTenantArchiveRecordQuery(
    Guid TenantId
) : IRequest<Result<TenantArchiveRecord>>;
