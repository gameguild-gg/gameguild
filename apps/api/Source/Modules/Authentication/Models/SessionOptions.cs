namespace GameGuild.Modules.Authentication;

/// <summary>
/// Configuration options for session management
/// </summary>
public class SessionOptions
{
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan? TrustedDeviceLifetime { get; set; } = TimeSpan.FromDays(30);

    public int MaxSessionsPerUser { get; set; } = 5;

    public bool RequireMfaForNewDevice { get; set; } = true;

    public bool RequireMfaForNewLocation { get; set; } = false;

    public TimeSpan SessionCleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
