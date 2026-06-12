namespace GameGuild.Resources;

/// <summary>
///     Persists usage-retention archive manifests to cold storage and backup providers.
/// </summary>
public interface IUsageRetentionArchiveSink
{
    Task<UsageArchiveManifest> ArchiveAsync(
        Guid? tenantId,
        ResourceUsageType? type,
        DateTime olderThan,
        int archivedRecordCount,
        CancellationToken cancellationToken = default);
}
