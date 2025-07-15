using GameGuild.Modules.Permissions;


namespace GameGuild.Common.Authorization;

/// <summary>
/// Backward-compatible resource permission attribute (infers permission type from entity)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : Entity {
  public override DACPermissionLevel PermissionLevel {
    get => DACPermissionLevel.Resource;
  }

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
