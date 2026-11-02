namespace GameGuild.Identity.Authentication;

/// <summary>
///     Server-side SAML protocol state (issue #268): the two single-use facts the
///     service provider must remember across the browser round-trip — outstanding
///     AuthnRequest IDs (<c>InResponseTo</c> correlation, each bound to the
///     <c>RelayState</c> issued with it) and consumed assertion IDs (replay
///     protection). The production implementation
///     (<see cref="PostgreSqlSamlProtocolStateStore" />) persists this in the shared
///     <c>gameguild.authentication</c> schema so flows started on one instance complete
///     on any other, and every operation is a single atomic SQL statement — concurrent
///     consumers of the same pending request or assertion ID can never both succeed.
/// </summary>
public interface ISamlProtocolStateStore
{
    /// <summary>
    ///     Records that <paramref name="requestId" /> was issued as an AuthnRequest ID for
    ///     provider <paramref name="slug" /> with <paramref name="relayState" /> as its
    ///     browser round-trip token, valid for <paramref name="timeToLive" />. The
    ///     RelayState is stored only as a fingerprint, never in the clear.
    /// </summary>
    Task SavePendingAuthnRequestAsync(string slug, string requestId, string relayState, TimeSpan timeToLive, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Consumes (single-use, atomically) the pending AuthnRequest
    ///     <paramref name="requestId" /> for provider <paramref name="slug" />, requiring
    ///     <paramref name="relayState" /> to match the value issued with the request.
    ///     Returns false when the request is unknown, already used, expired, or the
    ///     RelayState does not match — the response must then be rejected.
    /// </summary>
    Task<bool> ConsumePendingAuthnRequestAsync(string slug, string requestId, string relayState, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reserves (single-use, atomically) the assertion <paramref name="assertionId" />
    ///     for provider <paramref name="slug" /> for <paramref name="timeToLive" />.
    ///     Returns false when the assertion was already consumed — a replayed response.
    /// </summary>
    Task<bool> TryReserveAssertionIdAsync(string slug, string assertionId, TimeSpan timeToLive, CancellationToken cancellationToken = default);
}
