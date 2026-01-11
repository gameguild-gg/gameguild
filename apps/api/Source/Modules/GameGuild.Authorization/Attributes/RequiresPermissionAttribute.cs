namespace GameGuild.Authorization;

/// <summary>
///     Specifies that the action method or endpoint requires a specific permission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of <see cref="RequiresPermissionAttribute"/>.
    /// </summary>
    /// <param name="permissionName">The permission name required.</param>
    public RequiresPermissionAttribute(string permissionName)
    {
        PermissionName = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
    }

    /// <summary>
    ///     Gets the required permission name.
    /// </summary>
    public string PermissionName { get; }
}

/// <summary>
///     Alias for RequiresPermissionAttribute for backward compatibility.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of <see cref="RequirePermissionAttribute"/>.
    /// </summary>
    /// <param name="permissionName">The permission name required.</param>
    public RequirePermissionAttribute(string permissionName)
    {
        PermissionName = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
    }

    /// <summary>
    ///     Gets the required permission name.
    /// </summary>
    public string PermissionName { get; }
}

/// <summary>
///     Specifies that the action method or endpoint requires a specific permission on a resource.
///     This is a generic attribute that allows type-safe resource permission checks.
/// </summary>
/// <typeparam name="TPermission">The permission enum type for the resource.</typeparam>
/// <typeparam name="TResource">The resource entity type.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireResourcePermissionAttribute<TPermission, TResource> : Attribute, IResourcePermissionMarker
    where TPermission : struct, Enum
    where TResource : class
{
    /// <summary>
    ///     Initializes a new instance of <see cref="RequireResourcePermissionAttribute{TPermission, TResource}"/>.
    /// </summary>
    /// <param name="requiredPermission">The permission required on the resource.</param>
    /// <param name="resourceIdParameterName">The name of the route/query parameter containing the resource ID.</param>
    public RequireResourcePermissionAttribute(TPermission requiredPermission, string resourceIdParameterName = "id")
    {
        RequiredPermission = requiredPermission;
        ResourceIdParameterName = resourceIdParameterName ?? throw new ArgumentNullException(nameof(resourceIdParameterName));
    }

    /// <summary>
    ///     Gets the required permission.
    /// </summary>
    public TPermission RequiredPermission { get; }

    /// <summary>
    ///     Gets the name of the parameter containing the resource ID.
    /// </summary>
    public string ResourceIdParameterName { get; }

    /// <summary>
    ///     Gets the resource type.
    /// </summary>
    public Type ResourceType => typeof(TResource);

    /// <summary>
    ///     Gets the permission type.
    /// </summary>
    public Type PermissionEnumType => typeof(TPermission);
    
    // IResourcePermissionMarker implementation
    object IResourcePermissionMarker.RequiredPermission => RequiredPermission;
    string IResourcePermissionMarker.ResourceIdParameterName => ResourceIdParameterName;
}

/// <summary>
///     Alias without "Attribute" suffix for cleaner usage.
///     Usage: [RequireResourcePermission&lt;PermissionType, Entity&gt;(PermissionType.Read)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireResourcePermission<TPermission, TResource> : Attribute, IResourcePermissionMarker
    where TPermission : struct, Enum
    where TResource : class
{
    public RequireResourcePermission(TPermission requiredPermission, string resourceIdParameterName = "id")
    {
        RequiredPermission = requiredPermission;
        ResourceIdParameterName = resourceIdParameterName ?? throw new ArgumentNullException(nameof(resourceIdParameterName));
    }

    public TPermission RequiredPermission { get; }
    public string ResourceIdParameterName { get; }
    public Type ResourceType => typeof(TResource);
    public Type PermissionEnumType => typeof(TPermission);
    
    // IResourcePermissionMarker implementation
    object IResourcePermissionMarker.RequiredPermission => RequiredPermission;
    string IResourcePermissionMarker.ResourceIdParameterName => ResourceIdParameterName;
}

/// <summary>
///     Specifies that the action method requires a specific permission on a content type.
///     Content-type level permissions apply to all instances of the content type within a tenant.
/// </summary>
/// <typeparam name="TResource">The content type entity.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireContentTypePermissionAttribute<TResource> : Attribute, IContentTypePermissionMarker
    where TResource : class
{
    public RequireContentTypePermissionAttribute(object permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public object Permission { get; }
    public Type ResourceType => typeof(TResource);
}

/// <summary>
///     Alias without "Attribute" suffix for cleaner usage.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireContentTypePermission<TResource> : Attribute, IContentTypePermissionMarker
    where TResource : class
{
    public RequireContentTypePermission(object permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public object Permission { get; }
    public Type ResourceType => typeof(TResource);
}

/// <summary>
///     Specifies that the action method requires a specific tenant-level permission.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireTenantPermissionAttribute : Attribute
{
    public RequireTenantPermissionAttribute(object permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public object Permission { get; }
}

/// <summary>
///     Alias without "Attribute" suffix for cleaner usage.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireTenantPermission : Attribute
{
    public RequireTenantPermission(object permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    public object Permission { get; }
}

/// <summary>
///     Marker interface for resource permission attributes to enable runtime discovery.
/// </summary>
public interface IResourcePermissionMarker
{
    object RequiredPermission { get; }
    string ResourceIdParameterName { get; }
    Type ResourceType { get; }
    Type PermissionEnumType { get; }
}

/// <summary>
///     Marker interface for content-type permission attributes.
/// </summary>
public interface IContentTypePermissionMarker
{
    object Permission { get; }
    Type ResourceType { get; }
}

// NOTE: AuthorizeRequestAttribute is defined in Behaviors/AuthorizationBehavior.cs
// for CQRS commands/queries authorization.
