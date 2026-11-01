namespace GameGuild.Identity.Authentication;

/// <summary>
///     Thrown when an OAuth identity provider cannot be reached or is failing outright
///     (connection errors, timeouts, 5xx responses) as opposed to rejecting the
///     presented credentials. Carries no secrets: the provider display name and the
///     original exception are all it exposes.
///     Issue #1116: lets the API surface a distinct "provider down" outcome so clients
///     can offer the magic-link fallback to OAuth-only accounts instead of a bare 401.
/// </summary>
public sealed class OAuthProviderUnavailableException : Exception
{
    /// <summary>Display name of the provider that could not be reached (e.g. "GitHub").</summary>
    public string Provider { get; }

    public OAuthProviderUnavailableException(string provider, Exception innerException)
        : base($"OAuth provider '{provider}' is unavailable. Try again later, or sign in with a magic link.", innerException)
    {
        Provider = provider;
    }

    /// <summary>
    ///     Classifies whether an exception thrown during a provider call represents
    ///     provider unavailability rather than an authentication failure:
    ///     transport-level <see cref="HttpRequestException" /> without a status code
    ///     (DNS/refused/reset), provider 5xx responses, and HttpClient timeouts.
    ///     Genuine caller cancellations are already filtered by the caller because
    ///     <see cref="OperationCanceledException" /> (the base of
    ///     <see cref="TaskCanceledException" />) never reaches this check.
    /// </summary>
    public static bool IsProviderUnavailability(Exception exception) => exception switch
    {
        TimeoutException => true,
        TaskCanceledException => true, // HttpClient timeout (real cancellations never reach here)
        HttpRequestException { StatusCode: null } => true, // transport failure (DNS, refused, reset)
        HttpRequestException { StatusCode: { } status } when (int)status >= 500 => true, // provider outage page
        _ => false
    };
}
