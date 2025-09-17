namespace GameGuild.Authorization;

/// <summary> Resource-level DAC authorization attribute for GraphQL resolvers Checks permissions for specific content entries within a tenant </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireResourcePermissionAttribute<TPermission, TEntity> : DACAuthorizationAttribute where TPermission : ResourcePermission<TEntity> where TEntity : EntityBase {
  /// <summary> Initialize resource-level permission requirement </summary>
  /// <param name="requiredPermission"> The permission type required </param>
  /// <param name="resourceIdParameterName"> The parameter name containing the resource ID (default: "id") </param>
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") : base(requiredPermission, typeof(TEntity)) { ResourceIdParameterName = resourceIdParameterName; }

  public override DACPermissionLevel PermissionLevel { get => DACPermissionLevel.Resource; }

  public override Type PermissionType { get => typeof(TPermission); }
}
