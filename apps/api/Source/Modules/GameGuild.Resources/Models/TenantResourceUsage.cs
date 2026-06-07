namespace GameGuild.Resources;

/// <summary>
///     Represents current tenant resource usage across all resources
/// </summary>
public class TenantResourceUsage
{
    public Guid TenantId { get; set; }

    public Dictionary<string, long> CurrentUsage { get; set; } = new Dictionary<string, long>();

    public Dictionary<string, long> Limits { get; set; } = new Dictionary<string, long>();

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }
}
