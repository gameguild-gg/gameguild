namespace GameGuild.Modules.Authentication;

/// <summary>
/// Session termination reasons
/// </summary>
public enum SessionTerminationReason
{
    UserLogout,

    AdministrativeTermination,

    Expired,

    SecurityViolation,

    DeviceChanged,

    LocationChanged,

    MaxSessionsExceeded
}
