namespace GameGuild;

/// <summary> Attribute specifically for resource-level permission checks </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireDacResourcePermissionAttribute : RequireDacPermissionAttribute {
    public RequireDacResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameter) : base(requiredPermission) { ResourceIdParameter = resourceIdParameter; }
}