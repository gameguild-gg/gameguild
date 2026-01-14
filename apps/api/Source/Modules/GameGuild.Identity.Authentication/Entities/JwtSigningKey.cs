using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     JWT signing key for token generation and validation with automatic rotation support
/// </summary>
[Table("jwt_signing_keys")]
[Index(nameof(KeyId), IsUnique = true)]
[Index(nameof(IsActive))]
[Index(nameof(ExpiresAt))]
public class JwtSigningKey : EntityBase
{
    /// <summary>
    ///     Unique identifier for this key (used in JWT header 'kid' claim)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    ///     Base64-encoded key material (symmetric key for HS256)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string KeyMaterial { get; set; } = string.Empty;

    /// <summary>
    ///     Algorithm used with this key (HS256, RS256, etc.)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    ///     Whether this key is currently active for signing new tokens
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     When this key becomes valid for use
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    ///     When this key expires and should no longer be used for validation
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     When this key was rotated out (set to inactive)
    /// </summary>
    public DateTime? RotatedAt { get; set; }

    /// <summary>
    ///     Reason for rotation (scheduled, compromised, manual, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? RotationReason { get; set; }

    /// <summary>
    ///     Version number for tracking key generations
    /// </summary>
    public int KeyVersion { get; set; }

    /// <summary>
    ///     Create a new signing key with auto-generated key material
    /// </summary>
    public static JwtSigningKey CreateNew(int keyVersion, DateTime validFrom, TimeSpan validity)
    {
        var keyId = $"key-{Guid.NewGuid():N}";
        var keyBytes = new byte[64]; // 512-bit key for HS256
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        return new JwtSigningKey
        {
            Id = Guid.NewGuid(),
            KeyId = keyId,
            KeyMaterial = Convert.ToBase64String(keyBytes),
            Algorithm = "HS256",
            IsActive = false, // Activated separately
            ValidFrom = validFrom,
            ExpiresAt = validFrom.Add(validity),
            KeyVersion = keyVersion,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     Activate this key for signing
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    ///     Rotate this key out of active use
    /// </summary>
    public void Rotate(string reason)
    {
        IsActive = false;
        RotatedAt = DateTime.UtcNow;
        RotationReason = reason;
        Touch();
    }

    /// <summary>
    ///     Check if this key is currently valid for token validation
    /// </summary>
    public bool IsValidForValidation(DateTime now)
    {
        return now >= ValidFrom && now < ExpiresAt;
    }
}
