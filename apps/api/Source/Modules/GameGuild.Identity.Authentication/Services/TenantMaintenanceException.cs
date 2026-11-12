namespace GameGuild.Identity.Authentication;

/// <summary>
///     A sign-in was refused because the resolved tenant is in a maintenance window
///     (issue #1134). Thrown after tenant resolution, once the first factor is verified, so
///     the response never doubles as an account-existence oracle: only a user who actually
///     authenticated learns the tenant is closed. Tenant administrators and SystemAdmins
///     bypass the gate and never see this exception.
/// </summary>
public sealed class TenantMaintenanceException : Exception
{
    /// <summary>The tenant whose maintenance window refused the sign-in.</summary>
    public Guid TenantId { get; }

    /// <summary>
    ///     Suggested Retry-After (seconds), derived from the window's scheduled resume time
    ///     when one exists; null for open-ended windows (callers apply their own backoff).
    /// </summary>
    public int? RetryAfterSeconds { get; }

    public TenantMaintenanceException(Guid tenantId, int? retryAfterSeconds)
        : base(
            "This tenant is undergoing maintenance and sign-in is temporarily unavailable."
            + " Please try again later.")
    {
        TenantId = tenantId;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
