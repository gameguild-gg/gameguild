namespace GameGuild.Modules.Authentication;

/// <summary>
/// Common failure reasons for login attempts
/// </summary>
public static class AuthenticationFailureReasons
{
    public const string InvalidCredentials = "InvalidCredentials";

    public const string UserNotFound = "UserNotFound";

    public const string AccountLocked = "AccountLocked";

    public const string MfaRequired = "MfaRequired";

    public const string MfaFailed = "MfaFailed";

    public const string RateLimited = "RateLimited";

    public const string SuspiciousActivity = "SuspiciousActivity";

    public const string AccountDisabled = "AccountDisabled";

    public const string PasswordExpired = "PasswordExpired";

    public const string TenantAccess = "TenantAccess";

    public const string ValidationError = "ValidationError";

    public const string SystemError = "SystemError";
}
