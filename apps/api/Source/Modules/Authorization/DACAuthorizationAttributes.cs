using GameGuild.Authorization;


namespace GameGuild.Modules.Authorization;

/// <summary> Backward-compatible resource permission attribute (infers permission type from entity) </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireResourcePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : EntityBase {
  /// <summary> Initialize resource-level permission requirement with inferred permission type </summary>
  /// <param name="requiredPermission"> The permission type required </param>
  /// <param name="resourceIdParameterName"> The parameter name containing the resource ID (default: "id") </param>
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") : base(requiredPermission, typeof(TEntity)) { ResourceIdParameterName = resourceIdParameterName; }

  public override DACPermissionLevel PermissionLevel { get => DACPermissionLevel.Resource; }
}
