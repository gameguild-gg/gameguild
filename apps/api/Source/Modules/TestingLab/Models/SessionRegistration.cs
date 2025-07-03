using System.ComponentModel.DataAnnotations;
using GameGuild.Models;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.TestingLab.Models {
  public class SessionRegistration : BaseEntity {
    /// <summary>
    /// Foreign key to the session
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session
    /// </summary>
    public virtual TestingSession Session { get; set; } = null!;

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual Modules.User.Models.User User { get; set; } = null!;

    [Required]
    public RegistrationType RegistrationType { get; set; }

    public TeamRole? ProjectRole { get; set; }

    public string? RegistrationNotes { get; set; }

    [Required]
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Registered;

    public DateTime? AttendedAt { get; set; }
  }
}
