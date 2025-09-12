namespace GameGuild.Common;

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
