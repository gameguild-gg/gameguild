namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Audit log for tracking all permission changes and access attempts
/// </summary>
[Table("PermissionAuditLogs")]
[Index(nameof(UserId), Name = "IX_PermissionAuditLogs_UserId")]
[Index(nameof(TenantId), Name = "IX_PermissionAuditLogs_TenantId")]
[Index(nameof(ResourceId), Name = "IX_PermissionAuditLogs_ResourceId")]
[Index(nameof(PerformedAt), Name = "IX_PermissionAuditLogs_PerformedAt")]
[Index(nameof(Operation), Name = "IX_PermissionAuditLogs_Operation")]
public class PermissionAuditLog : EntityBase
{
    /// <summary>
    /// User ID affected by the permission change
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tenant ID where the permission change occurred
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Resource ID for resource-specific permissions
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Type of operation performed (Grant, Revoke, Expire, Check, Denied)
    /// </summary>
    [MaxLength(50)]
    public string Operation { get; set; } = null!;

    /// <summary>
    /// Permissions involved in the operation
    /// </summary>
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Optional reason for the operation
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// ID of the user who performed the operation
    /// </summary>
    public Guid? PerformedBy { get; set; }

    /// <summary>
    /// When the operation was performed
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the user who performed the operation
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client that performed the operation
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or metadata for the operation
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Layer of the permission system (Tenant, ContentType, Resource)
    /// </summary>
    [MaxLength(50)]
    public string? PermissionLayer { get; set; }

    /// <summary>
    /// Content type name for content-type permissions
    /// </summary>
    [MaxLength(100)]
    public string? ContentTypeName { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}