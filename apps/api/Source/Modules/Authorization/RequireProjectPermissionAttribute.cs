namespace GameGuild.Attributes;

/// <summary>
/// Attribute for project-specific permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireProjectPermissionAttribute : RequireDacResourcePermissionAttribute {
    public RequireProjectPermissionAttribute(PermissionType requiredPermission) : base(requiredPermission, "projectId") {
        ContentTypeName = "Project";
        ResourceOwnerIdProperty = "OwnerId";
    }
}