namespace GameGuild.SharedKernel.Configuration;

public class RateLimitingOptions : BaseOptions
{
    public bool EnableRateLimiting { get; set; } = false;

    public int Limit { get; set; } = 100;

    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(1);

    // Compatibility properties
    public int RequestsPerMinute { get; set; } = 60;

    public int BurstSize { get; set; } = 10;

    public string[ ] ExemptPaths { get; set; } = Array.Empty<string>();

    public static RateLimitingOptions CreateDefault() { return new RateLimitingOptions(); }
}
