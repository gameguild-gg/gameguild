namespace GameGuild.Resources.Models;

/// <summary>
///     Resource limit check response
/// </summary>
public class ResourceLimitCheckResponse
{
    public ResourceUsageType Type { get; set; }

    public long Current { get; set; }

    public long Limit { get; set; }

    public long CurrentUsage { get; set; }

    public long? SoftLimit { get; set; }

    public long? HardLimit { get; set; }

    public bool CanProceed { get; set; }

    public string Message { get; set; } = string.Empty;
}
