namespace GameGuild.Assets;

/// <summary>Evaluates folder restrictions after parent-resource access has succeeded.</summary>
public interface IAssetFolderAuthorizationService
{
    Task<bool> CanReadAsync(
        AssetReference reference,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> CanReadFolderAsync(
        AssetFolder folder,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
