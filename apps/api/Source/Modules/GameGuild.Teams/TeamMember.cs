using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Identity.Users;

namespace GameGuild.Teams;

[Table("project_collaboration_team_members")]
public sealed class TeamMember : EntityBase
{
    public Guid TeamId { get; set; }
    public Team? Team { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public TeamMemberAuthority Authority { get; set; } = TeamMemberAuthority.Member;

    [MaxLength(150)]
    public string? ProfessionalTitle { get; set; }

    public DateTime JoinedAt { get; set; } = SystemClock.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    internal static TeamMember Create(
        Guid tenantId,
        Guid teamId,
        Guid userId,
        TeamMemberAuthority authority,
        string? professionalTitle) => new()
    {
        TenantId = tenantId,
        TeamId = teamId,
        UserId = userId,
        Authority = authority,
        ProfessionalTitle = professionalTitle?.Trim()
    };

    internal void Activate(TeamMemberAuthority authority, string? professionalTitle)
    {
        Authority = authority;
        ProfessionalTitle = professionalTitle?.Trim();
        IsActive = true;
        LeftAt = null;
        Touch();
    }

    internal void ChangeAuthority(TeamMemberAuthority authority)
    {
        Authority = authority;
        Touch();
    }

    internal void Deactivate()
    {
        IsActive = false;
        LeftAt = SystemClock.UtcNow;
        Touch();
    }
}
