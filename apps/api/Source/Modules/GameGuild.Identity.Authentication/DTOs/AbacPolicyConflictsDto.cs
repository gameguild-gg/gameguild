namespace GameGuild.Identity.Authentication;

public abstract class AbacPolicyConflictsDto
{
    public Guid? TenantId { get; set; }

    public List<PolicyConflict> Conflicts { get; set; } = new List<PolicyConflict>();

    public List<PolicyOverlap> Overlaps { get; set; } = new List<PolicyOverlap>();

    public DateTime AnalyzedAt { get; set; }
}
