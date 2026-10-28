namespace GameGuild.Identity.Authentication;

/// <summary>
///     Persisted SAML 2.0 protocol state (issue #268): the two single-use facts the
///     service provider must remember across the browser round-trip — outstanding
///     AuthnRequest IDs (<c>InResponseTo</c> correlation, bound to the issued
///     <c>RelayState</c>) and consumed assertion IDs (replay protection). Rows live in
///     the shared <c>gameguild.authentication</c> schema so every application instance
///     sees the same state; consumption and reservation are single atomic SQL
///     statements (see <see cref="PostgreSqlSamlProtocolStateStore" />).
/// </summary>
public sealed class SamlProtocolState
{
    /// <summary>
    ///     Which single-use fact this row represents: <c>pending_request</c> (an issued
    ///     AuthnRequest awaiting its response) or <c>assertion_reservation</c> (a
    ///     consumed assertion ID held past its validity window for replay protection).
    /// </summary>
    public string StateKind { get; set; } = string.Empty;

    /// <summary>
    ///     The federation provider slug the state was issued for; state is never shared
    ///     across providers (tenant/provider isolation).
    /// </summary>
    public string ProviderSlug { get; set; } = string.Empty;

    /// <summary>
    ///     The AuthnRequest ID (pending requests) or assertion ID (reservations).
    /// </summary>
    public string StateKey { get; set; } = string.Empty;

    /// <summary>
    ///     SHA-256 fingerprint of the <c>RelayState</c> issued with the AuthnRequest
    ///     (pending requests; empty for assertion reservations). The callback must
    ///     present the same RelayState for the pending request to be consumable —
    ///     this is the provider-bound leg of the browser callback-state binding.
    /// </summary>
    public string RelayStateFingerprint { get; set; } = string.Empty;

    /// <summary>
    ///     When this row stops being valid (UTC). Expired pending requests fail closed;
    ///     expired reservations may be reclaimed for a fresh assertion ID.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    ///     When this row was created or last refreshed (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
