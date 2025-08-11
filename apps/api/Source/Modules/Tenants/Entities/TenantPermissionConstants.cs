using GameGuild.Modules.Permissions;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Constants for tenant permission system
/// </summary>
public static class TenantPermissionConstants {
  public const int MaxPermissionsPerGrant = 100;

  public const int MaxExpirationDays = 365;

  /// <summary>
  /// Default permissions that every user gets
  /// </summary>
  public static readonly PermissionType[] MinimalUserPermissions = [PermissionType.Comment, PermissionType.Vote, PermissionType.Share];
}
