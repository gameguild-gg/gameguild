using System.ComponentModel.DataAnnotations;

namespace GameGuild.Assets;

public sealed class AssetScopedAccessGrant : EntityBase
{
    public Guid AssetReferenceId { get; private set; }
    public Guid UserId { get; private set; }
    [Required, MaxLength(100)] public string ScopeType { get; private set; } = string.Empty;
    public Guid ScopeId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private AssetScopedAccessGrant() { }

    public static AssetScopedAccessGrant Create(
        Guid assetReferenceId,
        Guid userId,
        Guid tenantId,
        string scopeType,
        Guid scopeId,
        DateTime expiresAt,
        Guid grantedByUserId)
    {
        if (expiresAt <= SystemClock.UtcNow) throw new ArgumentException("Grant expiry must be in the future.", nameof(expiresAt));
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeType);
        return new AssetScopedAccessGrant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssetReferenceId = assetReferenceId,
            UserId = userId,
            ScopeType = scopeType.Trim(),
            ScopeId = scopeId,
            ExpiresAt = expiresAt,
            GrantedByUserId = grantedByUserId
        };
    }

    public bool IsActive => RevokedAt == null && ExpiresAt > SystemClock.UtcNow && DeletedAt == null;

    public void Revoke()
    {
        if (RevokedAt == null) RevokedAt = SystemClock.UtcNow;
        Touch();
    }
}
