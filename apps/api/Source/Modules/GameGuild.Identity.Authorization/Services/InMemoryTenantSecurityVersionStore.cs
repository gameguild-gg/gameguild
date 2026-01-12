using System.Collections.Concurrent;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     In-memory implementation of tenant security version store.
///     In production, use a distributed implementation (Redis/database).
/// </summary>
public sealed class InMemoryTenantSecurityVersionStore : ITenantSecurityVersionStore
{
    private readonly ConcurrentDictionary<string, long> _versions = new();

    /// <inheritdoc />
    public Task<long> GetVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var version = _versions.GetOrAdd(tenantId, 1L);
        return Task.FromResult(version);
    }

    /// <inheritdoc />
    public Task<long> IncrementVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var newVersion = _versions.AddOrUpdate(
            tenantId,
            2L,
            (_, current) => current + 1);

        return Task.FromResult(newVersion);
    }
}
