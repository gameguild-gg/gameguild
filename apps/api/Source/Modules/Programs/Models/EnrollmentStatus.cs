using System.ComponentModel;


namespace GameGuild.Modules.Programs;
/// <summary> Enrollment status enumeration </summary>
public enum EnrollmentStatus {
  Open = 0,

  Active = 1,

  Paused = 2,

  Cancelled = 3,

  Expired = 4,

  Completed = 5,
  [Description("Open for enrollment")] Open,
  //
  [Description("Closed for enrollment")] Closed,
  //
  [Description("Invite only")] InviteOnly,
  //
  [Description("Waitlist available")] Waitlist,
}
