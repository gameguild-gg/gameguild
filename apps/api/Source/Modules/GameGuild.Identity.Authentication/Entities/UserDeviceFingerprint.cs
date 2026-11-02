using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Per-user record of a device fingerprint that has already signed in at least once
///     (issue #1085). A session created with a fingerprint that has no row for its user
///     is a first-seen device and triggers the new-device sign-in alert.
/// </summary>
public class UserDeviceFingerprint
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    ///     Device fingerprint exactly as recorded on the user's sessions.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    ///     When this fingerprint was first seen for the user (the sign-in that triggered
    ///     the new-device alert, or the first session that recorded it).
    /// </summary>
    public DateTime FirstSeenAt { get; set; }

    /// <summary>
    ///     Last sign-in observed with this fingerprint.
    /// </summary>
    public DateTime LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
