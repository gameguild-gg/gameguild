namespace GameGuild.Identity.Authentication;

public abstract class PeriodicAccessReview
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Schedule { get; set; } = string.Empty;

    public string ReviewType { get; set; } = string.Empty;

    public List<Guid> ReviewerIds { get; set; } = new List<Guid>();

    public bool IsActive { get; set; }

    public DateTime? LastRunDate { get; set; }

    public DateTime? NextRunDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
