namespace GameGuild.Assets;

/// <summary>
/// Resolves inherited access against the authoritative parent resource module.
/// Implementations must load and validate the parent resource; caller-provided tenant IDs
/// are contextual hints and are never an authorization source.
/// </summary>
public interface IAssetParentAuthorizationResolver
{
    bool Supports(string resourceType);

    Task<bool> CanReadAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageAsync(
        Guid parentResourceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
