using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.DTOs;

public abstract class EffectivePermissionsDto
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? ResourceId { get; set; }

    public string? ContentType { get; set; }

    public List<PermissionType> EffectivePermissions { get; set; } = new List<PermissionType>();

    public List<PermissionInheritanceInfo> InheritanceChain { get; set; } = new List<PermissionInheritanceInfo>();

    public DateTime CalculatedAt { get; set; }
}
