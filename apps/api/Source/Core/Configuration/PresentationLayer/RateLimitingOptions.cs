namespace GameGuild;

public class RateLimitingOptions
{
    public int RequestsPerMinute { get; set; } = 60;

    public int BurstSize { get; set; } = 10;

    public string[] ExemptPaths { get; set; } = [];

    public void Validate()
    {
        if (RequestsPerMinute <= 0)
            throw new InvalidOperationException("Requests per minute must be greater than zero.");

        if (BurstSize <= 0)
            throw new InvalidOperationException("Burst size must be greater than zero.");

        if (ExemptPaths == null)
            throw new InvalidOperationException("Exempt paths cannot be null.");
    }
}
