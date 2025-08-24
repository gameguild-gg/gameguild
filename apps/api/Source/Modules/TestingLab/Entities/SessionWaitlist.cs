using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab {
  public class SessionWaitlist : Entity {
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
    public virtual User User { get; set; } = null!;

    [Required] public RegistrationType RegistrationType { get; set; }

    [Required] public int Position { get; set; }

    public string? RegistrationNotes { get; set; }
  }
}
