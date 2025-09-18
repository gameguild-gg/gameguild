using System.ComponentModel;

namespace GameGuild.Source.Modules.Programs.Models;

/// <summary>
/// Represents the enrollment status for a program enrollment
/// </summary>
public enum EnrollmentStatus {
  /// <summary>Open for enrollment</summary>
  [Description("Open for enrollment")]
  Open = 0,

  /// <summary>Currently active enrollment</summary>
  [Description("Active enrollment")]
  Active = 1,

  /// <summary>Enrollment is paused</summary>
  [Description("Paused enrollment")]
  Paused = 2,

  /// <summary>Enrollment was cancelled</summary>
  [Description("Cancelled enrollment")]
  Cancelled = 3,

  /// <summary>Enrollment has expired</summary>
  [Description("Expired enrollment")]
  Expired = 4,

  /// <summary>Enrollment is completed</summary>
  [Description("Completed enrollment")]
  Completed = 5,

  /// <summary>Closed for enrollment</summary>
  [Description("Closed for enrollment")]
  Closed = 6,

  /// <summary>Invite only enrollment</summary>
  [Description("Invite only")]
  InviteOnly = 7,

  /// <summary>Waitlist available</summary>
  [Description("Waitlist available")]
  Waitlist = 8
}
