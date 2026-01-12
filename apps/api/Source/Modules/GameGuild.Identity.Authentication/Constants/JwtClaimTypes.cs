namespace GameGuild.Identity.Authentication;

/// <summary>
///     Constants for custom JWT claim types used throughout the GameGuild application.
///     These supplement the standard claims from JwtRegisteredClaimNames and ClaimTypes.
/// </summary>
public static class JwtClaimTypes
{
    /// <summary>
    ///     Claim type for tenant identifier.
    ///     Used to identify which tenant the user is currently accessing.
    /// </summary>
    public const string TenantId = "tenant_id";

    /// <summary>
    ///     Claim type for tenant permission flags (first 64 bits).
    ///     Contains the user's tenant-level permissions as a bit flag.
    /// </summary>
    public const string TenantPermissionFlags1 = "tenant_permission_flags1";

    /// <summary>
    ///     Claim type for tenant permission flags (second 64 bits).
    ///     Contains the user's tenant-level permissions as a bit flag.
    /// </summary>
    public const string TenantPermissionFlags2 = "tenant_permission_flags2";

    /// <summary>
    ///     Claim type for username.
    ///     Used for display and user identification.
    /// </summary>
    public const string Username = "username";

    /// <summary>
    ///     Claim type for user's display name.
    /// </summary>
    public const string DisplayName = "display_name";

    /// <summary>
    ///     Claim type for user's avatar URL.
    /// </summary>
    public const string AvatarUrl = "avatar_url";

    /// <summary>
    ///     Claim type for MFA status.
    ///     Indicates whether the user has MFA enabled.
    /// </summary>
    public const string MfaEnabled = "mfa_enabled";

    /// <summary>
    ///     Claim type for session ID.
    ///     Used to track the specific session associated with this token.
    /// </summary>
    public const string SessionId = "session_id";
}
