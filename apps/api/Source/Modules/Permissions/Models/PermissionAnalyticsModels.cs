namespace GameGuild.Modules.Permissions.Models;

/// <summary>
/// Comprehensive report on permission usage patterns
/// </summary>
public class PermissionUsageReport
{
    /// <summary>
    /// Most frequently used permissions in the tenant
    /// </summary>
    public IEnumerable<PermissionUsageStatistic> MostUsedPermissions { get; set; } = Enumerable.Empty<PermissionUsageStatistic>();

    /// <summary>
    /// Permissions that were denied access attempts
    /// </summary>
    public IEnumerable<PermissionDenialStatistic> DeniedAttempts { get; set; } = Enumerable.Empty<PermissionDenialStatistic>();

    /// <summary>
    /// Recent permission changes (grants, revokes)
    /// </summary>
    public IEnumerable<PermissionChangeStatistic> PermissionChanges { get; set; } = Enumerable.Empty<PermissionChangeStatistic>();

    /// <summary>
    /// Permissions that exist but are never used
    /// </summary>
    public IEnumerable<PermissionType> UnusedPermissions { get; set; } = Enumerable.Empty<PermissionType>();

    /// <summary>
    /// Users with the most permission usage
    /// </summary>
    public IEnumerable<UserPermissionActivity> MostActiveUsers { get; set; } = Enumerable.Empty<UserPermissionActivity>();

    /// <summary>
    /// Peak usage times and patterns
    /// </summary>
    public PermissionUsagePattern UsagePatterns { get; set; } = new();

    /// <summary>
    /// Security-related statistics
    /// </summary>
    public PermissionSecurityReport SecurityReport { get; set; } = new();

    /// <summary>
    /// Report generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time period covered by this report
    /// </summary>
    public DateTimeRange Period { get; set; } = new();
}

/// <summary>
/// Statistics for a specific permission usage
/// </summary>
public class PermissionUsageStatistic
{
    public PermissionType Permission { get; set; }
    public int UsageCount { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
    public double AverageUsagePerUser { get; set; }
    public string PermissionLayer { get; set; } = null!;
}

/// <summary>
/// Statistics for permission denial attempts
/// </summary>
public class PermissionDenialStatistic
{
    public PermissionType Permission { get; set; }
    public int DenialCount { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime FirstDenied { get; set; }
    public DateTime LastDenied { get; set; }
    public string MostCommonReason { get; set; } = null!;
    public IEnumerable<string> TopDeniedResources { get; set; } = Enumerable.Empty<string>();
}

/// <summary>
/// Statistics for permission changes
/// </summary>
public class PermissionChangeStatistic
{
    public string Operation { get; set; } = null!; // Grant, Revoke, Expire
    public PermissionType Permission { get; set; }
    public int ChangeCount { get; set; }
    public DateTime LastChange { get; set; }
    public Guid? LastChangedBy { get; set; }
    public string Trend { get; set; } = null!; // Increasing, Decreasing, Stable
}

/// <summary>
/// User activity statistics
/// </summary>
public class UserPermissionActivity
{
    public Guid UserId { get; set; }
    public int TotalPermissionChecks { get; set; }
    public int SuccessfulChecks { get; set; }
    public int DeniedChecks { get; set; }
    public double SuccessRate { get; set; }
    public IEnumerable<PermissionType> MostUsedPermissions { get; set; } = Enumerable.Empty<PermissionType>();
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Permission usage patterns over time
/// </summary>
public class PermissionUsagePattern
{
    public Dictionary<int, int> HourlyUsage { get; set; } = new(); // Hour -> Usage count
    public Dictionary<DayOfWeek, int> DailyUsage { get; set; } = new(); // Day -> Usage count
    public Dictionary<string, int> MonthlyUsage { get; set; } = new(); // Month -> Usage count
    public int PeakHour { get; set; }
    public DayOfWeek PeakDay { get; set; }
    public string PeakMonth { get; set; } = null!;
}

/// <summary>
/// Security-related permission statistics
/// </summary>
public class PermissionSecurityReport
{
    public int TotalSecurityIncidents { get; set; }
    public int SuspiciousActivityAttempts { get; set; }
    public IEnumerable<SecurityIncident> RecentIncidents { get; set; } = Enumerable.Empty<SecurityIncident>();
    public IEnumerable<Guid> UsersWithHighDenialRates { get; set; } = Enumerable.Empty<Guid>();
    public int EscalatedPermissions { get; set; } // Permissions granted to users who previously didn't have them
    public int ExpiredPermissions { get; set; }
}

/// <summary>
/// Security incident information
/// </summary>
public class SecurityIncident
{
    public Guid? UserId { get; set; }
    public string IncidentType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = null!; // Low, Medium, High, Critical
    public bool IsResolved { get; set; }
}

/// <summary>
/// Date/time range for reports
/// </summary>
public class DateTimeRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public TimeSpan Duration => EndDate - StartDate;
    public bool IsValid => StartDate <= EndDate;
}