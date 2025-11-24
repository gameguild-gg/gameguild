using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for managing permission template versions and migrations.
///     Provides version control, comparison, and migration capabilities for templates.
/// </summary>
public interface IPermissionTemplateVersioningService
{
    // ==================== VERSION MANAGEMENT ====================

    /// <summary>
    ///     Creates a new version of a permission template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="userId">The user creating the version.</param>
    /// <param name="permissions">The permissions for this version.</param>
    /// <param name="changeNotes">Notes describing the changes.</param>
    /// <param name="changeType">The type of change (Major, Minor, Patch).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template version.</returns>
    Task<PermissionTemplateVersion> CreateVersionAsync(Guid templateId, Guid userId, string[ ] permissions, string? changeNotes, TemplateChangeType changeType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the complete version history for a template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all versions ordered by version number.</returns>
    Task<List<PermissionTemplateVersion>> GetVersionHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific version of a template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="versionNumber">The version number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template version, or null if not found.</returns>
    Task<PermissionTemplateVersion?> GetVersionAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the currently active version of a template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active version, or null if none is active.</returns>
    Task<PermissionTemplateVersion?> GetActiveVersionAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Activates a specific version of a template (deactivates others).
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="versionNumber">The version number to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateVersionAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Compares two versions of a template and returns the differences.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="fromVersion">The source version number.</param>
    /// <param name="toVersion">The target version number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed diff showing added/removed/changed permissions.</returns>
    Task<VersionDiff> CompareVersionsAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);

    // ==================== MIGRATION MANAGEMENT ====================

    /// <summary>
    ///     Plans a migration from one version to another.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="userId">The user planning the migration.</param>
    /// <param name="strategy">The migration strategy.</param>
    /// <param name="scheduledFor">When to execute the migration (null for immediate).</param>
    /// <param name="dryRun">Whether this is a dry run (test only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration plan.</returns>
    Task<PermissionTemplateMigration> PlanMigrationAsync(
        Guid templateId,
        int fromVersion,
        int toVersion,
        Guid userId,
        MigrationStrategy strategy,
        DateTime? scheduledFor,
        bool dryRun,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Executes a planned migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completed migration with results.</returns>
    Task<PermissionTemplateMigration> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs a dry run of a migration without applying changes.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results showing what would be changed.</returns>
    Task<DryRunResult> PerformDryRunAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancels a planned or scheduled migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rolls back a completed migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the migration history for a template.
    /// </summary>
    /// <param name="templateId">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of migrations ordered by date.</returns>
    Task<List<PermissionTemplateMigration>> GetMigrationHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific migration by ID.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration, or null if not found.</returns>
    Task<PermissionTemplateMigration?> GetMigrationByIdAsync(Guid migrationId, CancellationToken cancellationToken = default);
}
