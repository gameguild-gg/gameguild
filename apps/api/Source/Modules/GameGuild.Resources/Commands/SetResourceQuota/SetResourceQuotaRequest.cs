
namespace GameGuild.Resources;

/// <summary>
///     Request DTO for setting/updating a resource quota
/// </summary>
public class SetResourceQuotaRequest
{
    public Guid TenantId { get; set; }

    public ResourceUsageType Type { get; set; }

    public int? SoftLimit { get; set; }

    public int? HardLimit { get; set; }

    public ResourceQuotaPeriod Period { get; set; } = ResourceQuotaPeriod.Monthly;

    public bool IsActive { get; set; } = true;

    public TimeSpan? ResetTime { get; set; }
}
