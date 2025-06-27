using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.TestingLab.Models {
  public class SessionWaitlist : BaseEntity {
    private Guid _sessionId;

    private TestingSession _session = null!;

    private Guid _userId;

    private User.Models.User _user = null!;

    private RegistrationType _registrationType;

    private int _position;

    private string? _registrationNotes;

    /// <summary>
    /// Foreign key to the session
    /// </summary>
    public Guid SessionId {
      get => _sessionId;
      set => _sessionId = value;
    }

    /// <summary>
    /// Navigation property to the session
    /// </summary>
    public virtual TestingSession Session {
      get => _session;
      set => _session = value;
    }

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    public Guid UserId {
      get => _userId;
      set => _userId = value;
    }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual Modules.User.Models.User User {
      get => _user;
      set => _user = value;
    }

    [Required]
    public RegistrationType RegistrationType {
      get => _registrationType;
      set => _registrationType = value;
    }

    [Required]
    public int Position {
      get => _position;
      set => _position = value;
    }

    public string? RegistrationNotes {
      get => _registrationNotes;
      set => _registrationNotes = value;
    }
  }
}
