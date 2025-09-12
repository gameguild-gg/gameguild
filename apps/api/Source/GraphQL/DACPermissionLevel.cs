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
