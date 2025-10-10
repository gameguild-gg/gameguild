namespace GameGuild.Modules.DataArchival.DTOs;

/// <summary>
/// Data transfer object for archival policy.
/// </summary>
public record ArchivalPolicyDto(
    Guid Id,
    string Name,
    string Description,
    bool IsEnabled,
    Guid? TenantId,
    string EntityType,
    int CoolStorageAfterDays,
    int ArchiveStorageAfterDays,
    int DeleteAfterDays,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);