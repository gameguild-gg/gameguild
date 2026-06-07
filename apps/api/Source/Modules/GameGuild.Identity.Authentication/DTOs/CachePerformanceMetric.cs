namespace GameGuild.Identity.Authentication;

public abstract class CachePerformanceMetric
{
    public string Operation { get; set; } = string.Empty;

    public double AverageTime { get; set; }

    public int RequestCount { get; set; }

    public DateTime Timestamp { get; set; }
}
