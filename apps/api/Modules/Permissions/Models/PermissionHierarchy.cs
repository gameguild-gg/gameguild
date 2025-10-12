namespace GameGuild.Core.Domain.Permissions;

/// <summary> Permission hierarchy for debugging and audit </summary>
public class PermissionHierarchy
{
    public PermissionType Permission { get; set; }

    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? ResourceId { get; set; }

    public string? ContentTypeName { get; set; }

    public List<PermissionLayer> Layers { get; set; } = [];

    public PermissionResult FinalResult { get; set; } = new PermissionResult();
}
