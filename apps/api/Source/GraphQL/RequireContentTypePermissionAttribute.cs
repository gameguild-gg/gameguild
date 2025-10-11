namespace GameGuild.Authorization;

/// <summary> Content-type level DAC authorization attribute for GraphQL resolvers Checks permissions that apply to all entries of a specific content type within a tenant </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireContentTypePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : class {
  /// <summary> Initialize content-type level permission requirement </summary>
  /// <param name="requiredPermission"> The permission type required </param>
  public RequireContentTypePermissionAttribute(PermissionType requiredPermission) : base(requiredPermission, typeof(TEntity)) { }

  public override DACPermissionLevel PermissionLevel { get => DACPermissionLevel.ContentType; }
}
