namespace GameGuild.Assets;

/// <summary>
/// Resolves product-specific restrictions applied to an asset folder after parent access is granted.
/// </summary>
public interface IAssetFolderRestrictionAuthorizationResolver
{
    bool Supports(AssetFolderRestrictionMode restrictionMode, string parentResourceType);

    Task<bool> IsAuthorizedAsync(
        AssetFolder folder,
        Guid userId,
        CancellationToken cancellationToken = default);
}
