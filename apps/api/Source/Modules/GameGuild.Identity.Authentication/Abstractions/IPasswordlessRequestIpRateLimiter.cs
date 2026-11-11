namespace GameGuild.Identity.Authentication;

/// <summary>
///     Per-IP admission for passwordless sign-in delivery requests (magic links and
///     one-time email codes). Complements the per-email throttles with a shared per-IP
///     budget so one address cannot flood many different mailboxes (email bombing).
///     Implementations must fail open: an unavailable counter store must never break
///     the request path, matching the defense-in-depth posture of the per-email throttle.
/// </summary>
public interface IPasswordlessRequestIpRateLimiter
{
    /// <summary>
    ///     Consumes one request from the IP's budget and reports whether the request is
    ///     admitted. Requests without a client IP are always admitted.
    /// </summary>
    /// <param name="ipAddress">Client IP observed for the request; <c>null</c> admits.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the request may proceed; <c>false</c> when the IP exhausted its budget.</returns>
    Task<bool> TryAdmitRequestAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
