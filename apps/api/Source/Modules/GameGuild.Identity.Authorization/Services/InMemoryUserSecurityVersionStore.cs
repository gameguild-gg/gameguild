using System.Collections.Concurrent;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     In-memory implementation of user security version store.
///     Suitable for single-instance deployments.
/// </summary>
/// <remarks>
///     For multi-instance deployments, use a distributed implementation
///     backed by Redis or a database.
/// </remarks>
public sealed class InMemoryUserSecurityVersionStore : IUserSecurityVersionStore
{
    private readonly ConcurrentDictionary<Guid, long> _versions = new();

    /// <inheritdoc />
    public Task<long> GetVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var version = _versions.GetValueOrDefault(userId, 0);
        return Task.FromResult(version);
    }

    /// <inheritdoc />
    public Task<long> IncrementVersionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var newVersion = _versions.AddOrUpdate(userId, 1, (_, v) => v + 1);
        return Task.FromResult(newVersion);
    }

    /// <inheritdoc />
    public Task IncrementVersionsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            _versions.AddOrUpdate(userId, 1, (_, v) => v + 1);
        }
        return Task.CompletedTask;
    }
}
