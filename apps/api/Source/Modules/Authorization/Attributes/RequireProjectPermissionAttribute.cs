namespace GameGuild;

/// <summary> Attribute for project-specific permission checks </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireProjectPermissionAttribute : RequireDacResourcePermissionAttribute {
    public RequireProjectPermissionAttribute(PermissionType requiredPermission) : base(requiredPermission, "projectId") {
        ContentTypeName = "Project";
        ResourceOwnerIdProperty = "OwnerId";
    }
}