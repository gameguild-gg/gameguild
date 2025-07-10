namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Statistics model for user profiles
/// </summary>
public class UserProfileStatistics
{
    /// <summary>
    /// Total number of user profiles
    /// </summary>
    public int TotalUserProfiles { get; set; }
    
    /// <summary>
    /// Number of active (non-deleted) user profiles
    /// </summary>
    public int ActiveUserProfiles { get; set; }
    
    /// <summary>
    /// Number of deleted user profiles
    /// </summary>
    public int DeletedUserProfiles { get; set; }
    
    /// <summary>
    /// Number of user profiles created in the specified date range
    /// </summary>
    public int NewUserProfiles { get; set; }
    
    /// <summary>
    /// Number of user profiles updated in the specified date range
    /// </summary>
    public int UpdatedUserProfiles { get; set; }
    
    /// <summary>
    /// Average number of user profiles created per day in the date range
    /// </summary>
    public double AverageNewUserProfilesPerDay { get; set; }
    
    /// <summary>
    /// Most common display name patterns
    /// </summary>
    public Dictionary<string, int> DisplayNamePatterns { get; set; } = new();
    
    /// <summary>
    /// Distribution by tenant (if multi-tenant)
    /// </summary>
    public Dictionary<string, int> TenantDistribution { get; set; } = new();
}
