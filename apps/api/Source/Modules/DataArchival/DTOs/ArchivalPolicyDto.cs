using GameGuild.Modules.DataArchival.DTOs;
namespace GameGuild.Modules.DataArchival.DTOs;

/// <summary>
/// Data transfer object for archival policy.
/// </summary>
public class ArchivalPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public Guid? TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int CoolStorageAfterDays { get; set; }
    public int ArchiveStorageAfterDays { get; set; }
    public int? DeleteAfterDays { get; set; }
    public bool CompressOnArchive { get; set; }
    public bool EncryptOnArchive { get; set; }
    public int Priority { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextExecutionAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
