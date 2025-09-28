namespace GameGuild.Attributes;

/// <summary>
/// Attribute specifically for resource-level permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireDacResourcePermissionAttribute : RequireDacPermissionAttribute {
    public RequireDacResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameter) : base(requiredPermission) { ResourceIdParameter = resourceIdParameter; }
}