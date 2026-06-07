namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyConflictsDto
{
    public Guid? TenantId { get; set; }

    public List<PolicyConflict> Conflicts { get; set; } = new List<PolicyConflict>();

    public List<PolicyDependency> Dependencies { get; set; } = new List<PolicyDependency>();

    public DateTime AnalyzedAt { get; set; }
}
