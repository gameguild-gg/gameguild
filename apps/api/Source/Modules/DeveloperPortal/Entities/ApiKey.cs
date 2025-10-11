using GameGuild.Core.Domain;

namespace GameGuild.Modules.DeveloperPortal.Entities;

/// <summary>
/// Represents an API key for developer access.
/// </summary>
public class ApiKey : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID this API key belongs to.
    /// </summary>
    public override Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who owns this API key.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the name/description of this API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed API key value.
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prefix shown to users (first 8 chars).
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last 4 characters for identification.
    /// </summary>
    public string KeySuffix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scopes/permissions this key has.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rate limit (requests per minute).
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether this key is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when this key expires (null for no expiration).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when this key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP addresses allowed to use this key.
    /// </summary>
    public string? AllowedIpAddresses { get; set; }

    /// <summary>
    /// Gets or sets the referrer URLs allowed to use this key.
    /// </summary>
    public string? AllowedReferrers { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests made with this key.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed requests.
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets when the key was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets who revoked the key.
    /// </summary>
    public Guid? RevokedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for revocation.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Gets or sets the collection of API usage logs for this key.
    /// </summary>
    public ICollection<ApiUsageLog> UsageLogs { get; set; } = new List<ApiUsageLog>();

    /// <summary>
    /// Checks if the API key is currently valid.
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive || RevokedAt.HasValue)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Records usage of this API key.
    /// </summary>
    public void RecordUsage(bool success = true)
    {
        TotalRequests++;
        if (!success)
            FailedRequests++;

        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes this API key.
    /// </summary>
    public void Revoke(Guid userId, string reason)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = userId;
        RevocationReason = reason;
    }
}
