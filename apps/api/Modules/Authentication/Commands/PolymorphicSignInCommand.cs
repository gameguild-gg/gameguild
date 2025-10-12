using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command for polymorphic sign-in supporting multiple credential types (email, phone, username)
/// </summary>
public class PolymorphicSignInCommand : IRequest<Result<SignInResponse>>
{
    /// <summary>
    /// The credential identifier - can be email, phone number, or username
    /// </summary>
    public string Credential { get; init; } = string.Empty;

    /// <summary>
    /// The credential type for explicit specification (optional, auto-detected if not provided)
    /// </summary>
    public CredentialType? CredentialType { get; init; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional tenant context
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Optional device fingerprint for trusted device tracking
    /// </summary>
    public string? DeviceFingerprint { get; init; }
}

/// <summary>
/// Credential type enumeration for polymorphic authentication
/// </summary>
public enum CredentialType
{
    Email,
    Phone,
    Username
}
