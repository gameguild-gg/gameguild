namespace GameGuild.Modules.Resources;

/// <summary> Request to invite a user to a resource </summary>
public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;

    public PermissionType[ ] Permissions { get; set; } = [];

    public DateTime? ExpiresAt { get; set; }

    public string? Message { get; set; }

    public bool RequireAcceptance { get; set; } = true;
}
