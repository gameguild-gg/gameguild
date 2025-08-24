using GameGuild.Common;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab {
  /// <summary>
  /// Represents a project that is registered to be tested in a specific testing session
  /// </summary>
  public class SessionProject : Entity {
    /// <summary>
    /// Foreign key to the session
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session
    /// </summary>
    public virtual TestingSession Session { get; set; } = null!;

    /// <summary>
    /// Foreign key to the project
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Foreign key to the project version being tested
    /// </summary>
    public Guid? ProjectVersionId { get; set; }

    /// <summary>
    /// Navigation property to the project version
    /// </summary>
    public virtual ProjectVersion? ProjectVersion { get; set; }

    /// <summary>
    /// Notes about this project's participation in the session
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this project was registered for the session
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foreign key to the user who registered this project for the session
    /// </summary>
    public Guid RegisteredById { get; set; }

    /// <summary>
    /// Navigation property to the user who registered this project
    /// </summary>
    public virtual User RegisteredBy { get; set; } = null!;

    /// <summary>
    /// Whether this project is still active in this session
    /// </summary>
    public bool IsActive { get; set; } = true;
  }
}
