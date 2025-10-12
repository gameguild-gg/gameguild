namespace GameGuild.Modules.Authentication;

/// <summary>
/// Analysis of user login patterns
/// </summary>
public class UserSignInAnalysis
{
    public Guid UserId { get; set; }

    public bool IsNewUser { get; set; }

    public bool IsNewLocation { get; set; }

    public bool IsNewDevice { get; set; }

    public bool IsUnusualTime { get; set; }

    public int RecentSuccessfulLogins { get; set; }

    public int UniqueLocations { get; set; }

    public int UniqueDevices { get; set; }
}
