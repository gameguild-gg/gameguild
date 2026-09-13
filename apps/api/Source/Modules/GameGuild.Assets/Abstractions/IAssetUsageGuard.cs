namespace GameGuild.Assets;

/// <summary>Prevents removal of an asset still referenced by an authoritative module.</summary>
public interface IAssetUsageGuard
{
    Task<bool> IsInUseAsync(Guid assetReferenceId, CancellationToken cancellationToken = default);
}
