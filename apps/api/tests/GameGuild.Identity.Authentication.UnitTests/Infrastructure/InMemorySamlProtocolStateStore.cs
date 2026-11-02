namespace GameGuild.Identity.Authentication.UnitTests.Infrastructure;

/// <summary>
///     Deterministic in-memory <see cref="ISamlProtocolStateStore" /> for unit tests,
///     mirroring the production PostgreSQL store's single-use semantics exactly: a
///     pending AuthnRequest is consumable only once, only with the RelayState it was
///     issued with, and only inside its validity window — and a failed consumption
///     attempt (wrong RelayState) leaves the pending request intact, like the atomic
///     conditional DELETE it models.
/// </summary>
public sealed class InMemorySamlProtocolStateStore : ISamlProtocolStateStore
{
    private sealed record PendingEntry(string RelayState, DateTimeOffset ExpiresAt);

    private readonly object _gate = new();
    private readonly Dictionary<(string Slug, string RequestId), PendingEntry> _pendingRequests = new();
    private readonly Dictionary<(string Slug, string AssertionId), DateTimeOffset> _assertionReservations = new();

    public Task SavePendingAuthnRequestAsync(string slug, string requestId, string relayState, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _pendingRequests[(slug, requestId)] = new PendingEntry(relayState, DateTimeOffset.UtcNow + timeToLive);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ConsumePendingAuthnRequestAsync(string slug, string requestId, string relayState, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_pendingRequests.TryGetValue((slug, requestId), out var entry) ||
                entry.ExpiresAt <= DateTimeOffset.UtcNow ||
                !string.Equals(entry.RelayState, relayState, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _pendingRequests.Remove((slug, requestId));
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryReserveAssertionIdAsync(string slug, string assertionId, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_assertionReservations.TryGetValue((slug, assertionId), out var expiresAt) &&
                expiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult(false);
            }

            _assertionReservations[(slug, assertionId)] = DateTimeOffset.UtcNow + timeToLive;
            return Task.FromResult(true);
        }
    }
}
