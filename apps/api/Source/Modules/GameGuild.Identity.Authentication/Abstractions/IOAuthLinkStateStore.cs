namespace GameGuild.Identity.Authentication;

/// <summary>
///     Server-side store for OAuth account-link flow state parameters (issue #1181).
///     The authorize step records the per-request <c>state</c> it embedded in the provider
///     authorization URL; the callback step round-trip validates the caller-echoed state
///     against that record and consumes it (one-shot), so CSRF protection no longer rests
///     solely on the calling client.
/// </summary>
public interface IOAuthLinkStateStore
{
    /// <summary>
    ///     Records the state issued to a user for a provider's link flow. Issuing a new state
    ///     for the same (user, provider) replaces any pending one.
    /// </summary>
    Task SaveAsync(Guid userId, string provider, string state, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates that <paramref name="state" /> is the pending state issued to the user for
    ///     the provider, and consumes it — a state value can complete at most one link.
    ///     Returns false for unknown, expired, already-used, or mismatched states.
    /// </summary>
    Task<bool> ValidateAndConsumeAsync(Guid userId, string provider, string state, CancellationToken cancellationToken = default);
}
