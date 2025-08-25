using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab;

public class TestingSession : Entity {
  /// <summary> Foreign key to the testing request </summary>
  public Guid TestingRequestId { get; set; }

  /// <summary> Navigation property to the testing request </summary>
  public virtual TestingRequest TestingRequest { get; set; } = null!;

  /// <summary> Foreign key to the testing location </summary>
  public Guid LocationId { get; set; }

  /// <summary> Navigation property to the testing location </summary>
  public virtual TestingLocation Location { get; set; } = null!;

  [Required][MaxLength(255)] public string SessionName { get; set; } = string.Empty;

  [Required] public DateTime SessionDate { get; set; }

  [Required] public DateTime StartTime { get; set; }

  [Required] public DateTime EndTime { get; set; }

  [Required] public int MaxTesters { get; set; }

  [Required] public int MaxProjects { get; set; } = 1;

  public int RegisteredTesterCount { get; set; } = 0;

  public int RegisteredProjectMemberCount { get; set; } = 0;

  public int RegisteredProjectCount { get; set; } = 0;

  [Required] public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

  /// <summary> Foreign key to the session manager </summary>
  public Guid ManagerId { get; set; }

  /// <summary> Navigation property to the session manager </summary>
  public virtual User Manager { get; set; } = null!;

  /// <summary> Additional foreign key to the session manager (for backward compatibility) </summary>
  public Guid ManagerUserId { get; set; }

  /// <summary> Foreign key to the user who created this session </summary>
  public Guid CreatedById { get; set; }

  /// <summary> Navigation property to the user who created this session </summary>
  public virtual User CreatedBy { get; set; } = null!;

  /// <summary> Navigation property to session registrations (users registered for this session) </summary>
  public virtual ICollection<SessionRegistration> Registrations { get; set; } = new List<SessionRegistration>();

  /// <summary> Navigation property to session projects (projects registered to be tested in this session) </summary>
  public virtual ICollection<SessionProject> SessionProjects { get; set; } = new List<SessionProject>();
}
