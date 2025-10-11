using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for managing permission template versions and migrations
/// </summary>
public class PermissionTemplateVersioningService : IPermissionTemplateVersioningService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionTemplateVersioningService> _logger;

    public PermissionTemplateVersioningService(ApplicationDbContext context, ILogger<PermissionTemplateVersioningService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PermissionTemplateVersion> CreateVersionAsync(Guid templateId, Guid userId, PermissionType[] permissions, string? changeNotes, TemplateChangeType changeType, CancellationToken cancellationToken = default)
    {
        var template = await _context.Set<PermissionTemplate>().FindAsync(new object[] { templateId }, cancellationToken);
        if (template == null) throw new InvalidOperationException("Template not found");

        var latestVersion = await GetActiveVersionAsync(templateId, cancellationToken);
        var newVersionNumber = (latestVersion?.Version ?? 0) + 1;

        var version = new PermissionTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Version = newVersionNumber,
            Name = template.Name,
            Description = template.Description,
            Permissions = permissions,
            IsActive = true,
            CreatedByUserId = userId,
            ChangeNotes = changeNotes,
            ChangeType = changeType,
            PreviousVersion = latestVersion?.Version,
            PermissionHash = PermissionTemplateVersion.CalculatePermissionHash(permissions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (latestVersion != null)
        {
            var diff = version.CompareWith(latestVersion);
            version.AddedPermissions = diff.AddedPermissions;
            version.RemovedPermissions = diff.RemovedPermissions;
            version.UnchangedPermissions = diff.UnchangedPermissions;
            latestVersion.IsActive = false;
        }

        _context.Set<PermissionTemplateVersion>().Add(version);
        await _context.SaveChangesAsync(cancellationToken);
        return version;
    }

    public async Task<IEnumerable<PermissionTemplateVersion>> GetVersionHistoryAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PermissionTemplateVersion>()
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionTemplateVersion?> GetVersionAsync(Guid templateId, int version, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PermissionTemplateVersion>()
            .FirstOrDefaultAsync(v => v.TemplateId == templateId && v.Version == version, cancellationToken);
    }

    public async Task<PermissionTemplateVersion?> GetActiveVersionAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PermissionTemplateVersion>()
            .Where(v => v.TemplateId == templateId && v.IsActive)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ActivateVersionAsync(Guid templateId, int version, CancellationToken cancellationToken = default)
    {
        var versions = await _context.Set<PermissionTemplateVersion>()
            .Where(v => v.TemplateId == templateId)
            .ToListAsync(cancellationToken);

        foreach (var v in versions)
            v.IsActive = v.Version == version;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<VersionDiff> CompareVersionsAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
    {
        var from = await GetVersionAsync(templateId, fromVersion, cancellationToken);
        var to = await GetVersionAsync(templateId, toVersion, cancellationToken);
        if (from == null || to == null) throw new InvalidOperationException("Version not found");
        return to.CompareWith(from);
    }

    public async Task<PermissionTemplateMigration> PlanMigrationAsync(Guid templateId, int fromVersion, int toVersion, Guid userId, MigrationStrategy strategy, DateTime? scheduledFor, bool dryRun, CancellationToken cancellationToken = default)
    {
        var diff = await CompareVersionsAsync(templateId, fromVersion, toVersion, cancellationToken);

        var migration = new PermissionTemplateMigration
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Status = MigrationStatus.Planned,
            Strategy = strategy,
            ScheduledFor = scheduledFor,
            InitiatedByUserId = userId,
            IsDryRun = dryRun,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<PermissionTemplateMigration>().Add(migration);
        await _context.SaveChangesAsync(cancellationToken);
        return migration;
    }

    public async Task<PermissionTemplateMigration> ExecuteMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default)
    {
        var migration = await GetMigrationByIdAsync(migrationId, cancellationToken);
        if (migration == null) throw new InvalidOperationException("Migration not found");

        migration.Status = MigrationStatus.InProgress;
        migration.StartedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Implementation would apply migration logic here

        migration.Status = MigrationStatus.Completed;
        migration.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return migration;
    }

    public async Task<DryRunResult> PerformDryRunAsync(Guid templateId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
    {
        var diff = await CompareVersionsAsync(templateId, fromVersion, toVersion, cancellationToken);

        return new DryRunResult
        {
            EstimatedSuccessCount = 0,
            EstimatedFailureCount = 0,
            EstimatedSkippedCount = 0,
            IsRecommended = !diff.IsBreakingChange,
            RecommendationReason = diff.IsBreakingChange ? "Breaking changes detected" : "Safe migration"
        };
    }

    public async Task CancelMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default)
    {
        var migration = await GetMigrationByIdAsync(migrationId, cancellationToken);
        if (migration != null)
        {
            migration.Status = MigrationStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RollbackMigrationAsync(Guid migrationId, CancellationToken cancellationToken = default)
    {
        var migration = await GetMigrationByIdAsync(migrationId, cancellationToken);
        if (migration != null)
        {
            migration.Status = MigrationStatus.RolledBack;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<PermissionTemplateMigration>> GetMigrationHistoryAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PermissionTemplateMigration>()
            .Where(m => m.TemplateId == templateId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionTemplateMigration?> GetMigrationByIdAsync(Guid migrationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PermissionTemplateMigration>().FindAsync(new object[] { migrationId }, cancellationToken);
    }
}
