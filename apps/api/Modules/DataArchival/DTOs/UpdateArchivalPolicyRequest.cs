namespace GameGuild.Modules.DataArchival.DTOs;

/// <summary>
/// Request to update an existing archival policy.
/// </summary>
public record UpdateArchivalPolicyRequest(
    string? Name,
    string? Description,
    int? RetentionDays,
    int? ArchiveAfterDays,
    int? DeleteAfterDays,
    string? StorageTier,
    bool? CompressionEnabled,
    bool? EncryptionEnabled,
    bool? IsEnabled
);
