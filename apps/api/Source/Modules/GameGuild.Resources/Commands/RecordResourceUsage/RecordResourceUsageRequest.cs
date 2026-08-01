
namespace GameGuild.Resources;

/// <summary>
///     Request DTO for recording resource usage
/// </summary>
public class RecordResourceUsageRequest
{
    public Guid TenantId { get; set; }

    public ResourceUsageType ResourceUsageType { get; set; }

    public long Count { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public string? Metadata { get; set; }
}
