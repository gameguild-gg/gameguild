namespace GameGuild.Core.Services;

public class RateLimitCheckResult
{
    public bool IsAllowed { get; private set; }

    public string? Reason { get; private set; }

    public TimeSpan? RetryAfter { get; private set; }

    private RateLimitCheckResult(bool isAllowed, string? reason = null, TimeSpan? retryAfter = null)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        RetryAfter = retryAfter;
    }

    public static RateLimitCheckResult Allow() => new(true);

    public static RateLimitCheckResult Deny(string reason, TimeSpan? retryAfter = null) => new(false, reason, retryAfter);
}
