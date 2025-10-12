namespace GameGuild.Modules.DataArchival.DTOs;

/// <summary>
/// Request to create a new archival policy.
/// </summary>
public record CreateArchivalPolicyRequest(
    Guid? TenantId,
    string Name,
    string Description,
    string EntityType,
    int RetentionDays,
    int ArchiveAfterDays,
    int DeleteAfterDays,
    string StorageTier,
    bool CompressionEnabled,
    bool EncryptionEnabled
);
