
namespace GameGuild.Resources;

/// <summary>
///     Request DTO for resetting resource usage
/// </summary>
public class ResetResourceUsageRequest
{
    public Guid TenantId { get; set; }

    public ResourceUsageType? ResourceUsageType { get; set; }
}
