namespace GameGuild.Identity.Authentication;

/// <summary>
///     Multi-step authentication flow steps
/// </summary>
public enum AuthenticationStep { PrimaryCredential, MfaVerification, DeviceTrust, RiskChallenge }
