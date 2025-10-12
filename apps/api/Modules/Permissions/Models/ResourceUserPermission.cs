using GameGuild.Core.Domain.Permissions;

namespace GameGuild.Modules.Resources;

/// <summary> User and their permissions on a resource </summary>
public class ResourceUserPermission
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public PermissionType[ ] Permissions { get; set; } = [];

    public DateTime GrantedAt { get; set; }

    public Guid GrantedByUserId { get; set; }

    public string GrantedByUserName { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public bool IsOwner { get; set; }

    public PermissionSource Source { get; set; }
}
