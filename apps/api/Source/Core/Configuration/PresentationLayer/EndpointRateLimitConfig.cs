namespace GameGuild;

public class EndpointRateLimitConfig
{
    public int RequestsPerMinute { get; set; }

    public int BurstSize { get; set; }

    public bool ApplyToUser { get; set; } = true;

    public bool ApplyToIp { get; set; } = true;

    public string[ ] ExemptRoles { get; set; } = [];
}