using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request DTO for local sign-in
/// </summary>
public class LocalSignInRequest
{
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant ID to use for the sign-in. If not provided, will use the first available tenant for the user
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Device fingerprint for trusted device tracking
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    // Server-only account resolution. These internal properties cannot be bound from JSON or advertised in OpenAPI.
    internal bool CredentialResolutionFailed { get; init; }
    internal Guid? ResolvedUserId { get; init; }
    internal AuthenticationTimingOrigin? TimingOrigin { get; init; }

    /// <summary>
    ///     Alias for Email to support polymorphic sign-in scenarios
    /// </summary>
    public string EmailOrUsername { get => Email; }
}
