using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     API key for programmatic access with scoped permissions
/// </summary>
[Table("api_keys")]
[Index(nameof(KeyHash), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(TenantId))]
[Index(nameof(IsActive))]
[Index(nameof(ExpiresAt))]
public class ApiKey : EntityBase
{
    /// <summary>
    ///     User who owns this API key
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Display name for the key
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     SHA-256 hash of the API key (never store plaintext)
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    ///     Prefix of the key for identification (first 8 chars, e.g., "gg_live_")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    ///     Scopes granted to this key (comma-separated)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this key is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     When this key expires (null = never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Last time this key was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    ///     Number of times this key has been used
    /// </summary>
    public long UsageCount { get; set; }

    /// <summary>
    ///     IP address restriction (null = no restriction)
    /// </summary>
    [MaxLength(100)]
    public string? IpWhitelist { get; set; }

    /// <summary>
    ///     When this key was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    ///     Reason for revocation
    /// </summary>
    [MaxLength(200)]
    public string? RevocationReason { get; set; }

    /// <summary>
    ///     Create a new API key
    /// </summary>
    public static (ApiKey key, string plaintext) Create(
        Guid userId,
        Guid tenantId,
        string name,
        string[] scopes,
        DateTime? expiresAt = null,
        string? ipWhitelist = null)
    {
        // Generate secure random key: gg_live_<32 random chars>
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var randomPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);

        var plaintext = $"gg_live_{randomPart}";
        var keyHash = ComputeHash(plaintext);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = "gg_live_",
            Scopes = string.Join(",", scopes),
            IsActive = true,
            ExpiresAt = expiresAt,
            IpWhitelist = ipWhitelist,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return (apiKey, plaintext);
    }

    /// <summary>
    ///     Validate a plaintext key against this entity
    /// </summary>
    public bool ValidateKey(string plaintext)
    {
        var hash = ComputeHash(plaintext);
        return hash == KeyHash;
    }

    /// <summary>
    ///     Check if key is currently valid
    /// </summary>
    public bool IsValid()
    {
        if (!IsActive) return false;
        if (RevokedAt.HasValue) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
        return true;
    }

    /// <summary>
    ///     Check if key has a specific scope
    /// </summary>
    public bool HasScope(string scope)
    {
        var scopes = Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return scopes.Contains(scope, StringComparer.OrdinalIgnoreCase) ||
               scopes.Contains("*"); // Wildcard scope
    }

    /// <summary>
    ///     Record usage of this key
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        UsageCount++;
        Touch();
    }

    /// <summary>
    ///     Revoke this key
    /// </summary>
    public void Revoke(string reason)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        Touch();
    }

    /// <summary>
    ///     Compute SHA-256 hash of plaintext key
    /// </summary>
    private static string ComputeHash(string plaintext)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    ///     Get list of scopes as array
    /// </summary>
    public string[] GetScopes()
    {
        return Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}
