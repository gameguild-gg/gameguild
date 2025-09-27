namespace GameGuild.Modules.Authentication;

/// <summary>
/// Device trust record for managing trusted devices
/// </summary>
public class TrustedDevice : EntityBase
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Device fingerprint
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name for the device
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Device information
    /// </summary>
    public string DeviceInfo { get; set; } = string.Empty; // JSON

    /// <summary>
    /// When device was trusted
    /// </summary>
    public DateTime TrustedAt { get; set; }

    /// <summary>
    /// Last time device was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Whether trust is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When trust expires (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// IP addresses associated with this device
    /// </summary>
    public string? AssociatedIpAddresses { get; set; } // JSON array

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

    public bool IsValid => IsActive && !IsExpired;
}
