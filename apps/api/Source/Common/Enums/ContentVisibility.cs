namespace GameGuild.Common.Enums;

/// <summary>
/// Content visibility levels
/// </summary>
public enum ContentVisibility {
  /// <summary>
  /// Only visible to the owner
  /// </summary>
  Private = 0,

  /// <summary>
  /// Visible to team members only
  /// </summary>
  Team = 1,

  /// <summary>
  /// Visible to all users in the tenant
  /// </summary>
  Tenant = 2,

  /// <summary>
  /// Publicly visible to everyone
  /// </summary>
  Public = 3,

  /// <summary>
  /// Hidden from public view but accessible via direct link
  /// </summary>
  Unlisted = 4,
}