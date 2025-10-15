using GameGuild.Common;
using GameGuild.Modules.Teams.Models;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab;

public class SessionRegistration : Entity {
  /// <summary> Foreign key to the session </summary>
  public Guid SessionId { get; set; }

  /// <summary> Navigation property to the session </summary>
  public virtual TestingSession Session { get; set; } = null!;

  /// <summary> Foreign key to the user </summary>
  public Guid UserId { get; set; }

  /// <summary> Navigation property to the user </summary>
  public virtual User User { get; set; } = null!;

  [Required] public RegistrationType RegistrationType { get; set; }

  public TeamRole? ProjectRole { get; set; }

  public string? RegistrationNotes { get; set; }

  [Required] public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Registered;

  public DateTime? AttendedAt { get; set; }
}
