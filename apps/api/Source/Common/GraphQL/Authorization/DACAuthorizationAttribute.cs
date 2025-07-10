using GameGuild.Modules.Permissions.Models;


namespace GameGuild.Common.Authorization;

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
