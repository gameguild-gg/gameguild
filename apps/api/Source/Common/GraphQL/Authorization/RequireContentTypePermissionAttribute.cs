using GameGuild.Modules.Permissions.Models;


namespace GameGuild.Common.Authorization;

/// <summary>
/// Content-type level DAC authorization attribute for GraphQL resolvers
/// Checks permissions that apply to all entries of a specific content type within a tenant
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireContentTypePermissionAttribute<TEntity> : DACAuthorizationAttribute where TEntity : class {
  public override DACPermissionLevel PermissionLevel {
    get => DACPermissionLevel.ContentType;
  }

  /// <summary>
  /// Initialize content-type level permission requirement
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  public RequireContentTypePermissionAttribute(PermissionType requiredPermission) : base(
    requiredPermission,
    typeof(TEntity)
  ) { }
}
