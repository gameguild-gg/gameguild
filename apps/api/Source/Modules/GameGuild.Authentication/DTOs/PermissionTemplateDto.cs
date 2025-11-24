using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.DTOs;

public abstract class PermissionTemplateDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public List<PermissionType> TenantPermissions { get; set; } = new List<PermissionType>();

    public Dictionary<string, List<PermissionType>> ContentTypePermissions { get; set; } = new Dictionary<string, List<PermissionType>>();

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
