using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Looks up users for resource sharing without coupling the authorization module to the users module.
/// </summary>
public interface IResourceShareUserLookup
{
    Task<ResourceShareUser?> FindByEmailAsync(TenantId tenantId, string email, CancellationToken cancellationToken = default);

    Task<ResourceShareUser?> FindByIdAsync(TenantId tenantId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed record ResourceShareUser(Guid UserId, string Email, string? DisplayName = null);

public sealed class NullResourceShareUserLookup : IResourceShareUserLookup
{
    public Task<ResourceShareUser?> FindByEmailAsync(TenantId tenantId, string email, CancellationToken cancellationToken = default)
        => Task.FromResult<ResourceShareUser?>(null);

    public Task<ResourceShareUser?> FindByIdAsync(TenantId tenantId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<ResourceShareUser?>(null);
}
