namespace GameGuild.Assets;

/// <summary>
/// Validates the authoritative parent resource and optional virtual folder before an upload.
/// Client-supplied resource and tenant identifiers are never treated as authorization.
/// </summary>
public interface IAssetUploadAuthorizationService
{
    Task<bool> CanUploadAsync(
        string? parentResourceType,
        Guid? parentResourceId,
        Guid? folderId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
