namespace GameGuild.Attributes;

/// <summary>
/// Attribute specifically for content-type permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireContentTypePermissionAttribute : RequireDacPermissionAttribute {
    public RequireContentTypePermissionAttribute(PermissionType requiredPermission, string contentTypeName) : base(requiredPermission) { ContentTypeName = contentTypeName; }
}