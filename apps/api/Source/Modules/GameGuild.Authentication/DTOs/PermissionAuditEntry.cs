using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.DTOs;

public abstract class PermissionAuditEntry
{
    public Guid Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string Action { get; set; } = string.Empty; // "Grant", "Revoke", "Check"

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public PermissionType Permission { get; set; }

    public string Level { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? ContentType { get; set; }

    public string? PerformedBy { get; set; }

    public string? Reason { get; set; }
}
