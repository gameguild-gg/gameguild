namespace GameGuild.Authentication.Enums;

/// <summary>
///     Supported MFA methods
/// </summary>
public enum MfaMethod { Totp = 1, BackupCode = 2, Sms = 3, Email = 4 }
