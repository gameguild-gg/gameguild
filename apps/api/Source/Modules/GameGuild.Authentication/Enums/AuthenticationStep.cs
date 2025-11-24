namespace GameGuild.Authentication.Enums;

/// <summary>
///     Multi-step authentication flow steps
/// </summary>
public enum AuthenticationStep { PrimaryCredential, MfaVerification, DeviceTrust, RiskChallenge }
