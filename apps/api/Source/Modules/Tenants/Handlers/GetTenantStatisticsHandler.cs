using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for getting tenant statistics
/// </summary>
public class GetTenantStatisticsHandler(
  ApplicationDbContext context,
  ILogger<GetTenantStatisticsHandler> logger
) : IQueryHandler<GetTenantStatisticsQuery, Common.Result<TenantStatistics>> {
  public async Task<Common.Result<TenantStatistics>> Handle(GetTenantStatisticsQuery request, CancellationToken cancellationToken) {
    try {
      var allTenants = context.Resources.OfType<Tenant>();
      var activeTenants = allTenants.Where(t => t.DeletedAt == null);

      var statistics = new TenantStatistics {
        TotalTenants = await activeTenants.CountAsync(cancellationToken),
        ActiveTenants = await activeTenants.CountAsync(t => t.IsActive, cancellationToken),
        InactiveTenants = await activeTenants.CountAsync(t => !t.IsActive, cancellationToken),
        DeletedTenants = await allTenants.CountAsync(t => t.DeletedAt != null, cancellationToken),
        OldestTenantCreatedAt = await activeTenants.MinAsync(t => (DateTime?)t.CreatedAt, cancellationToken),
        NewestTenantCreatedAt = await activeTenants.MaxAsync(t => (DateTime?)t.CreatedAt, cancellationToken),
      };

      logger.LogInformation(
        "Generated tenant statistics: {Total} total, {Active} active, {Inactive} inactive, {Deleted} deleted",
        statistics.TotalTenants,
        statistics.ActiveTenants,
        statistics.InactiveTenants,
        statistics.DeletedTenants
      );

      return Result.Success(statistics);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error generating tenant statistics");

      return Result.Failure<TenantStatistics>(
        Common.Error.Failure("Tenant.StatisticsFailed", "Failed to generate tenant statistics")
      );
    }
  }
}
