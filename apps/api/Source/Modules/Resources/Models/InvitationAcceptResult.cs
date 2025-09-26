namespace GameGuild.Modules.Resources;

/// <summary> Result of accepting an invitation </summary>
public class InvitationAcceptResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public PermissionType[ ] GrantedPermissions { get; set; } = [];
}
