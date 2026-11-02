using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     PostgreSQL-backed SAML protocol state (issue #268) in the shared
///     <c>gameguild.authentication</c> schema, so every application instance observes the
///     same single-use facts. All three operations are single atomic SQL statements:
///     issuance is an upsert, pending-request consumption is a conditional
///     <c>DELETE</c> that also matches the issued RelayState fingerprint, and assertion
///     reservation is an <c>INSERT ... ON CONFLICT</c> that only takes over an expired
///     row. Concurrent consumers on any instance therefore serialize in the database —
///     exactly one of them ever observes <c>true</c>.
/// </summary>
public sealed class PostgreSqlSamlProtocolStateStore(IApplicationDbContext context) : ISamlProtocolStateStore
{
    /// <summary>Row kind for an outstanding AuthnRequest awaiting its response.</summary>
    public const string PendingRequestKind = "pending_request";

    /// <summary>Row kind for a consumed assertion ID held for replay protection.</summary>
    public const string AssertionReservationKind = "assertion_reservation";

    public async Task SavePendingAuthnRequestAsync(string slug, string requestId, string relayState, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);
        ArgumentNullException.ThrowIfNull(relayState);
        var database = RequirePostgreSqlContext();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now + timeToLive;
        var fingerprint = RelayStateFingerprint(relayState);

        // Expired pending requests are reclaimed opportunistically; the upsert itself is
        // a single atomic statement (a re-issued request ID refreshes its window).
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "gameguild.authentication"."saml_protocol_state"
            WHERE state_kind = {PendingRequestKind} AND expires_at <= {now}
            """, cancellationToken).ConfigureAwait(false);

        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "gameguild.authentication"."saml_protocol_state"
                (state_kind, provider_slug, state_key, relay_state_fingerprint, expires_at, created_at)
            VALUES ({PendingRequestKind}, {slug}, {requestId}, {fingerprint}, {expiresAt}, {now})
            ON CONFLICT (state_kind, provider_slug, state_key)
            DO UPDATE SET relay_state_fingerprint = EXCLUDED.relay_state_fingerprint,
                          expires_at = EXCLUDED.expires_at,
                          created_at = EXCLUDED.created_at
            """, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ConsumePendingAuthnRequestAsync(string slug, string requestId, string relayState, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);
        ArgumentNullException.ThrowIfNull(relayState);
        var database = RequirePostgreSqlContext();

        // One atomic conditional DELETE: the row must exist, be unexpired, and carry the
        // RelayState fingerprint issued with the AuthnRequest. Unknown, expired,
        // already-consumed, or foreign RelayState all fail closed; concurrent consumers
        // race on the same row so exactly one of them deletes it.
        var changed = await database.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "gameguild.authentication"."saml_protocol_state"
            WHERE state_kind = {PendingRequestKind}
              AND provider_slug = {slug}
              AND state_key = {requestId}
              AND relay_state_fingerprint = {RelayStateFingerprint(relayState)}
              AND expires_at > {DateTimeOffset.UtcNow}
            """, cancellationToken).ConfigureAwait(false);

        return changed == 1;
    }

    public async Task<bool> TryReserveAssertionIdAsync(string slug, string assertionId, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(assertionId);
        var database = RequirePostgreSqlContext();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now + timeToLive;

        // Atomic add-if-absent: the upsert only wins when no reservation exists or the
        // existing one has expired past its replay window. A live reservation leaves the
        // row untouched (0 rows affected) — a replayed assertion.
        var changed = await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "gameguild.authentication"."saml_protocol_state"
                (state_kind, provider_slug, state_key, relay_state_fingerprint, expires_at, created_at)
            VALUES ({AssertionReservationKind}, {slug}, {assertionId}, {string.Empty}, {expiresAt}, {now})
            ON CONFLICT (state_kind, provider_slug, state_key)
            DO UPDATE SET expires_at = EXCLUDED.expires_at,
                          created_at = EXCLUDED.created_at
            WHERE "saml_protocol_state".expires_at <= {now}
            """, cancellationToken).ConfigureAwait(false);

        return changed == 1;
    }

    private DbContext RequirePostgreSqlContext() =>
        context is DbContext databaseContext && databaseContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? databaseContext
            : throw new InvalidOperationException("SAML protocol state requires the shared PostgreSQL context.");

    /// <summary>SHA-256 hex fingerprint of the RelayState issued with an AuthnRequest.</summary>
    private static string RelayStateFingerprint(string relayState) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(relayState)));
}
