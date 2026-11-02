namespace GameGuild.Identity.Authentication;

/// <summary>
///     Session termination reasons
/// </summary>
public enum SessionTerminationReason
{
    UserLogout,

    AdministrativeTermination,

    Expired,

    SecurityViolation,

    DeviceChanged,

    LocationChanged,

    MaxSessionsExceeded,

    MfaDisabled,

    /// <summary>
    ///     The user's password was changed or reset (issue #278): every active session is
    ///     terminated as part of the credential-change revocation battery.
    /// </summary>
    PasswordChanged
}
