namespace GameGuild.Resources;

/// <summary>
/// Indicates that a configured fail-closed rate limiter could not verify request admission.
/// </summary>
public sealed class RateLimitBackendUnavailableException : Exception
{
    public RateLimitBackendUnavailableException(string operation, Exception innerException)
        : base($"The distributed rate-limit store is unavailable during {operation}.", innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
    }
}
