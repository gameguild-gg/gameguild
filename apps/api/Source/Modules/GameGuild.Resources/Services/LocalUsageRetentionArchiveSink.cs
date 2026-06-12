using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Local archive sink that produces deterministic cold-storage and backup references.
/// </summary>
public sealed class LocalUsageRetentionArchiveSink(ILogger<LocalUsageRetentionArchiveSink> logger) : IUsageRetentionArchiveSink
{
    public Task<UsageArchiveManifest> ArchiveAsync(
        Guid? tenantId,
        ResourceUsageType? type,
        DateTime olderThan,
        int archivedRecordCount,
        CancellationToken cancellationToken = default)
    {
        var createdAt = SystemClock.UtcNow;
        var scope = $"{tenantId?.ToString("N") ?? "global"}:{type?.ToString() ?? "all"}:{olderThan:yyyyMMdd}";
        var manifest = new UsageArchiveManifest(
            tenantId,
            type,
            olderThan,
            archivedRecordCount,
            $"local-cold-storage:{scope}",
            $"local-backup:{scope}",
            createdAt);

        logger.LogInformation(
            "Created usage archive manifest {StorageReference} with backup {BackupReference} for {Count} records",
            manifest.StorageReference,
            manifest.BackupReference,
            archivedRecordCount);

        return Task.FromResult(manifest);
    }
}
