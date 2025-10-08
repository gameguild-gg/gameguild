using GameGuild.Modules.Permissions.Constants;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for managing permission template versions and migrations
/// </summary>
public interface IPermissionTemplateVersioningService
{
    // Version Management
    Task<PermissionTemplateVersion> CreateVersionAsync(Guid templateId, Guid userId, PermissionType[] permissions, string? changeNotes, TemplateChangeType changeType, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionTemplateVersion>> GetVersionHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<PermissionTemplateVersion?> GetVersionAsync(Guid templateId, int version, CancellationToken cancellationToken = default);
    Task<PermissionTemplateVersion?> GetActiveVersionAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task ActivateVersionAsync(Guid templateId, int version, CancellationToken cancellationToken = default);
    Task<VersionDiff> CompareVersionsAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);

    // Migration Management
    Task<PermissionTemplateMigration> PlanMigrationAsync(Guid templateId, int fromVersion, int toVersion, Guid userId, MigrationStrategy strategy, DateTime? scheduledFor, bool dryRun, CancellationToken cancellationToken = default);
    Task<PermissionTemplateMigration> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);
    Task<DryRunResult> PerformDryRunAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);
    Task CancelMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);
    Task RollbackMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionTemplateMigration>> GetMigrationHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<PermissionTemplateMigration?> GetMigrationByIdAsync(Guid migrationId, CancellationToken cancellationToken = default);
}
