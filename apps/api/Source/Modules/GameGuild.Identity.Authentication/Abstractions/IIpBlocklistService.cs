namespace GameGuild.Identity.Authentication;

/// <summary>A proactive blocklist entry for a known-attacker IP address (issue #1089).</summary>
public sealed record IpBlocklistEntry
{
    /// <summary>The exact client IP address (IPv4 or IPv6) covered by the entry.</summary>
    public required string IpAddress { get; init; }

    /// <summary>Free-form operator note recorded when the entry was added.</summary>
    public string? Reason { get; init; }

    /// <summary>Identity of the administrator who added the entry.</summary>
    public string? AddedBy { get; init; }

    /// <summary>When the entry was added (UTC).</summary>
    public DateTimeOffset AddedAt { get; init; }

    /// <summary>When the entry stops matching (UTC); <c>null</c> means permanent.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
///     Proactive blocklist of known-attacker IP addresses, stored in the distributed cache
///     (Redis in production) so every API instance shares one list, and managed through the
///     admin endpoint. Lookups fail open: an unavailable cache never locks every user out —
///     matches are then simply not observed until the store recovers.
/// </summary>
public interface IIpBlocklistService
{
    /// <summary>
    ///     Returns the active entry for the IP, or <c>null</c> when the address is not
    ///     blocklisted (including entries that already expired).
    /// </summary>
    Task<IpBlocklistEntry?> FindEntryAsync(string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Lists every stored entry, expired entries filtered out.</summary>
    Task<IReadOnlyList<IpBlocklistEntry>> ListEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds or replaces the entry for its IP address.</summary>
    Task AddOrUpdateEntryAsync(IpBlocklistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Removes the entry for the IP address. Returns whether an entry was removed.</summary>
    Task<bool> RemoveEntryAsync(string ipAddress, CancellationToken cancellationToken = default);
}
