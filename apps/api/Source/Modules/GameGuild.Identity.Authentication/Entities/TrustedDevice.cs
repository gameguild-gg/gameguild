using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Device trust record for managing trusted devices
/// </summary>
public class TrustedDevice
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    ///     Device fingerprint
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    ///     Friendly name for the device
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    ///     Device information (JSON)
    /// </summary>
    [MaxLength(2000)]
    public string DeviceInfo { get; set; } = string.Empty;

    /// <summary>
    ///     When device was trusted
    /// </summary>
    public DateTime TrustedAt { get; set; }

    /// <summary>
    ///     Last time device was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    ///     Whether trust is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     When trust expires (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     IP addresses associated with this device (JSON array)
    /// </summary>
    [MaxLength(1000)]
    public string? AssociatedIpAddresses { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsExpired { get => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value; }

    public bool IsValid { get => IsActive && !IsExpired; }
}
