namespace GameGuild.Resources;

public sealed record UsageArchiveManifest(
    Guid? TenantId,
    ResourceUsageType? ResourceType,
    DateTime OlderThan,
    int ArchivedRecordCount,
    string StorageReference,
    string BackupReference,
    DateTime CreatedAt);
