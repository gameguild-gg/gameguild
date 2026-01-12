using GameGuild.Identity.Authorization;
﻿namespace GameGuild.Identity.Authentication;

public abstract class PermissionAnalyticsDto
{
    public Guid TenantId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int TotalUsers { get; set; }

    public int TotalPermissions { get; set; }

    public Dictionary<PermissionType, int> PermissionUsage { get; set; } = new Dictionary<PermissionType, int>();

    public List<PermissionTrend> UsageTrends { get; set; } = new List<PermissionTrend>();

    public List<TopPermissionUser> TopUsers { get; set; } = new List<TopPermissionUser>();
}
