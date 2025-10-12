namespace GameGuild.Modules.Authentication;

/// <summary>
/// Orchestration layer for coordinating authentication flows across multiple providers and credential types
/// </summary>
public interface IAuthenticationOrchestrationService
{
    /// <summary>
    /// Orchestrates polymorphic sign-in flow with automatic credential type detection
    /// </summary>
    Task<Result<SignInResponse>> PolymorphicSignInAsync(PolymorphicSignInRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates social provider sign-in with fallback mechanisms
    /// </summary>
    Task<Result<SignInResponse>> SocialSignInAsync(SocialSignInRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates multi-step authentication flows (e.g., MFA after primary auth)
    /// </summary>
    Task<Result<SignInResponse>> MultiStepAuthenticationAsync(MultiStepAuthRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the credential type from the provided identifier
    /// </summary>
    CredentialType DetectCredentialType(string credential);

    /// <summary>
    /// Validates credential format before attempting authentication
    /// </summary>
    Result ValidateCredentialFormat(string credential, CredentialType type);

    /// <summary>
    /// Coordinates account linking during social sign-in
    /// </summary>
    Task<Result<SignInResponse>> LinkAccountAsync(string userId, string provider, string providerUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for polymorphic sign-in
/// </summary>
public class PolymorphicSignInRequest
{
    public string Credential { get; set; } = string.Empty;
    public CredentialType? ExplicitType { get; set; }
    public string Password { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Request for social sign-in with multiple providers
/// </summary>
public class SocialSignInRequest
{
    public SocialProvider Provider { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public bool LinkIfExists { get; set; }
}

/// <summary>
/// Request for multi-step authentication
/// </summary>
public class MultiStepAuthRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public AuthenticationStep Step { get; set; }
    public Dictionary<string, string> StepData { get; set; } = new();
}

/// <summary>
/// Social authentication providers
/// </summary>
public enum SocialProvider
{
    Google,
    Facebook,
    Microsoft,
    GitHub,
    Twitter,
    LinkedIn,
    Apple
}

/// <summary>
/// Multi-step authentication flow steps
/// </summary>
public enum AuthenticationStep
{
    PrimaryCredential,
    MfaVerification,
    DeviceTrust,
    RiskChallenge
}
