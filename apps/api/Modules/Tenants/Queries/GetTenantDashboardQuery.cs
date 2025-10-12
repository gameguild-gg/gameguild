using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get comprehensive tenant dashboard data </summary>
public class GetTenantDashboardQuery : IQuery<Result<TenantDashboardDto>>
{
    public Guid TenantId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary> Comprehensive dashboard data for a tenant </summary>
public class TenantDashboardDto
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    
    // Member statistics
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    public int NewMembersThisMonth { get; init; }
    
    // Usage statistics
    public long StorageUsed { get; init; }
    public long StorageLimit { get; init; }
    public int ApiCallsThisMonth { get; init; }
    public int ApiCallsLimit { get; init; }
    
    // Settings summary
    public TenantSettings? Settings { get; init; }
    
    // Recent activity
    public IEnumerable<RecentActivityDto> RecentActivities { get; init; } = Enumerable.Empty<RecentActivityDto>();
}

/// <summary> Recent activity item </summary>
public class RecentActivityDto
{
    public DateTime Timestamp { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
}