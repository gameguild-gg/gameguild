using GameGuild.Modules.Permissions;


namespace GameGuild.Common.Authorization;

/// <summary>
/// Tenant-level DAC authorization attribute for GraphQL resolvers
/// Checks permissions that apply to all content types within a tenant
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireTenantPermissionAttribute : DACAuthorizationAttribute {
  public override DACPermissionLevel PermissionLevel {
    get => DACPermissionLevel.Tenant;
  }

  /// <summary>
  /// Initialize tenant-level permission requirement
  /// </summary>
  /// <param name="requiredPermission">The permission type required</param>
  public RequireTenantPermissionAttribute(PermissionType requiredPermission) :
    base(requiredPermission, typeof(object)) // No specific entity type for tenant-level
  { }
}
