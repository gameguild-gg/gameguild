using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Resources;


namespace GameGuild.Common.Authorization;

/// <summary>
/// Permission levels for the 3-layer DAC system
/// </summary>
public enum DACPermissionLevel {
  /// <summary>
  /// Tenant-wide permissions - applies to all content types within a tenant
  /// </summary>
  Tenant,

  /// <summary>
  /// Content-type permissions - applies to all entries of a specific content type within a tenant
  /// </summary>
  ContentType,

  /// <summary>
  /// Resource-level permissions - applies to specific content entries within a tenant
  /// </summary>
  Resource,
}

/// <summary>
/// Base attribute for 3-layer DAC authorization in GraphQL resolvers
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public abstract class DACAuthorizationAttribute(PermissionType requiredPermission, Type entityType) : Attribute {
  /// <summary>
  /// The permission level (Tenant, ContentType, or Resource)
  /// </summary>
  public abstract DACPermissionLevel PermissionLevel { get; }

  /// <summary>
  /// The required permission type for the operation
  /// </summary>
  public PermissionType RequiredPermission { get; } = requiredPermission;

  /// <summary>
  /// The entity type being operated on
  /// </summary>
  public Type EntityType { get; } = entityType;

  /// <summary>
  /// The permission type (for resource-level permissions)
  /// </summary>
  public virtual Type? PermissionType { get; }

  /// <summary>
  /// The parameter name containing the resource ID (for resource-level permissions)
  /// </summary>
  public virtual string ResourceIdParameterName { get; protected set; } = "id";
}

/// <summary>
/// Tenant-level DAC authorization attribute for GraphQL resolvers
/// Checks permissions that apply to all content types within a tenant
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireTenantPermissionAttribute : DACAuthorizationAttribute {
  public override DACPermissionLevel PermissionLevel => DACPermissionLevel.Tenant;

  /// <summary>
  /// Initialize tenant-level permission requirement
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  public RequireTenantPermissionAttribute(PermissionType requiredPermission) :
    base(requiredPermission, typeof(object)) // No specific entity type for tenant-level
  { }
}

/// <summary>
/// Content-type level DAC authorization attribute for GraphQL resolvers
/// Checks permissions that apply to all entries of a specific content type within a tenant
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireContentTypePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : class {
  public override DACPermissionLevel PermissionLevel => DACPermissionLevel.ContentType;

  /// <summary>
  /// Initialize content-type level permission requirement
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  public RequireContentTypePermissionAttribute(PermissionType requiredPermission) : base(
    requiredPermission,
    typeof(TEntity)
  ) { }
}

/// <summary>
/// Resource-level DAC authorization attribute for GraphQL resolvers
/// Checks permissions for specific content entries within a tenant
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TPermission, TEntity> : DACAuthorizationAttribute
  where TPermission : ResourcePermission<TEntity> where TEntity : Entity {
  public override DACPermissionLevel PermissionLevel => DACPermissionLevel.Resource;

  public override Type PermissionType => typeof(TPermission);

  /// <summary>
  /// Initialize resource-level permission requirement
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  /// <param name="resourceIdParameterName">The parameter name containing the resource ID (default: "id")</param>
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") :
    base(requiredPermission, typeof(TEntity)) {
    ResourceIdParameterName = resourceIdParameterName;
  }
}

/// <summary>
/// Backward-compatible resource permission attribute (infers permission type from entity)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : Entity {
  public override DACPermissionLevel PermissionLevel => DACPermissionLevel.Resource;

  /// <summary>
  /// Initialize resource-level permission requirement with inferred permission type
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  /// <param name="resourceIdParameterName">The parameter name containing the resource ID (default: "id")</param>
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") :
    base(requiredPermission, typeof(TEntity)) {
    ResourceIdParameterName = resourceIdParameterName;
  }
}
