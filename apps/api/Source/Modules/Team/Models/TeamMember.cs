using GameGuild.Common.Entities;

namespace GameGuild.Models {
  public class TeamMember : BaseEntity {
    private Guid _teamId;

    private string _userId = string.Empty;

    private TeamRole _role;

    private string? _invitedBy;

    private MemberStatus _status = MemberStatus.Pending;

    private Team? _team;

    public Guid TeamId {
      get => _teamId;
      set => _teamId = value;
    }

    public string UserId {
      get => _userId;
      set => _userId = value;
    } // External user reference

    public TeamRole Role {
      get => _role;
      set => _role = value;
    }

    public string? InvitedBy {
      get => _invitedBy;
      set => _invitedBy = value;
    } // user_id who sent invitation (nullable)

    public MemberStatus Status {
      get => _status;
      set => _status = value;
    }

    public Team? Team {
      get => _team;
      set => _team = value;
    }
  }
}
