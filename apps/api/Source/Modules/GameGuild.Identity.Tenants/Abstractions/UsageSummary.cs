namespace GameGuild.Identity.Tenants;

/// <summary>
///     Summary of usage data for a tenant
/// </summary>
public abstract record UsageSummary
{
    public Guid TenantId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public int TotalActions { get; init; }

    public decimal TotalCost { get; init; }

    public Dictionary<string, int> ActionCounts { get; init; } = new Dictionary<string, int>();

    public Dictionary<string, int> ResourceCounts { get; init; } = new Dictionary<string, int>();

    public Dictionary<string, decimal> ResourceCosts { get; init; } = new Dictionary<string, decimal>();
}
