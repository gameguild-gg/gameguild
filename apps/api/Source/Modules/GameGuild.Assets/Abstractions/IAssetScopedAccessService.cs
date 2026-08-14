namespace GameGuild.Assets;

/// <summary>Issues exact, temporary access to explicitly selected asset references.</summary>
public interface IAssetScopedAccessService
{
    Task GrantAsync(
        IReadOnlyCollection<Guid> assetReferenceIds,
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid scopeId,
        DateTime expiresAt,
        Guid grantedByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveGrantAsync(
        Guid assetReferenceId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task RevokeScopeAsync(string scopeType, Guid scopeId, CancellationToken cancellationToken = default);
}
