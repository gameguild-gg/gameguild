namespace GameGuild.Common.Enums;

/// <summary>
/// Types of projects in the game guild platform
/// </summary>
public enum ProjectType {
  /// <summary>
  /// Video game project
  /// </summary>
  Game = 0,

  /// <summary>
  /// Development tool or utility
  /// </summary>
  Tool = 1,

  /// <summary>
  /// Art or design project
  /// </summary>
  Art = 2,

  /// <summary>
  /// Music or audio project
  /// </summary>
  Music = 3,

  /// <summary>
  /// Educational content or tutorial
  /// </summary>
  Educational = 4,

  /// <summary>
  /// Plugin or extension
  /// </summary>
  Plugin = 5,

  /// <summary>
  /// Template or starter project
  /// </summary>
  Template = 6,

  /// <summary>
  /// Library or framework
  /// </summary>
  Library = 7,

  /// <summary>
  /// Other type of project
  /// </summary>
  Other = 99,
}

/// <summary>
/// Development status of a project
/// </summary>
public enum DevelopmentStatus {
  /// <summary>
  /// Project is in planning phase
  /// </summary>
  Planning = 0,

  /// <summary>
  /// Project is in early development
  /// </summary>
  InDevelopment = 1,

  /// <summary>
  /// Project is in alpha testing
  /// </summary>
  Alpha = 2,

  /// <summary>
  /// Project is in beta testing
  /// </summary>
  Beta = 3,

  /// <summary>
  /// Project is released and active
  /// </summary>
  Released = 4,

  /// <summary>
  /// Project is completed and no longer in active development
  /// </summary>
  Completed = 5,

  /// <summary>
  /// Project is on hold or paused
  /// </summary>
  OnHold = 6,

  /// <summary>
  /// Project has been cancelled
  /// </summary>
  Cancelled = 7,

  /// <summary>
  /// Project is archived or deprecated
  /// </summary>
  Archived = 8,
}

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
