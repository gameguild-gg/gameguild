namespace GameGuild.Modules.Resources;

/// <summary> Pending invitation to access a resource </summary>
public class ResourceInvitation
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public PermissionType[ ] Permissions { get; set; } = [];

    public DateTime InvitedAt { get; set; }

    public Guid InvitedByUserId { get; set; }

    public string InvitedByUserName { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public string? Message { get; set; }

    public InvitationStatus Status { get; set; }
}
