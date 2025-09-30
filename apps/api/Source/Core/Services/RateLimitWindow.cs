namespace GameGuild.Core.Services;

public class RateLimitWindow
{
    public DateTimeOffset WindowStart { get; set; } = DateTimeOffset.UtcNow;

    public int RequestCount { get; set; } = 0;

    public int TokensRemaining { get; set; } = 0;
}
