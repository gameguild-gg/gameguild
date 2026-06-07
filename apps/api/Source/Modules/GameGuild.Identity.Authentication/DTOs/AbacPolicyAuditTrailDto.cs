namespace GameGuild.Identity.Authentication;

public abstract class AbacPolicyAuditTrailDto
{
    public Guid PolicyId { get; set; }

    public List<PolicyAuditEntry> Entries { get; set; } = new List<PolicyAuditEntry>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
