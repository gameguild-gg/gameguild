using GameGuild.Common;


namespace GameGuild.Modules.Teams.Models {
  public class TeamMember : Entity {
    public Guid TeamId { get; set; }

    public string UserId { get; set; } = string.Empty; // External user reference

    public TeamRole Role { get; set; }

    public string? InvitedBy { get; set; } // user_id who sent the invitation (nullable)

    public MemberStatus Status { get; set; } = MemberStatus.Pending;

    public Team? Team { get; set; }
  }
}
