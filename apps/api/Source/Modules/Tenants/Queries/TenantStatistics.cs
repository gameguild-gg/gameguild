namespace GameGuild.Modules.Tenants;

/// <summary>
/// Statistics for tenant data
/// </summary>
public class TenantStatistics
{
  public int TotalTenants { get; init; }
  public int ActiveTenants { get; init; }
  public int InactiveTenants { get; init; }
  public int DeletedTenants { get; init; }
  public DateTime? OldestTenantCreatedAt { get; init; }
  public DateTime? NewestTenantCreatedAt { get; init; }
  public DateTime LastUpdatedAt { get; init; } = DateTime.UtcNow;
}
