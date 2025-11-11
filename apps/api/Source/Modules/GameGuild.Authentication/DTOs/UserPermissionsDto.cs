using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.DTOs;

// Permission Response DTOs
public abstract class UserPermissionsDto
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public List<PermissionType> TenantPermissions { get; set; } = new List<PermissionType>();

    public Dictionary<string, List<PermissionType>> ContentTypePermissions { get; set; } = new Dictionary<string, List<PermissionType>>();

    public Dictionary<Guid, List<PermissionType>> ResourcePermissions { get; set; } = new Dictionary<Guid, List<PermissionType>>();

    public DateTime LastUpdated { get; set; }
}

// Permission Analytics DTOs

// Permission Template DTOs
