using GameGuild.Database;
using GameGuild.CQRS;
using GameGuild.Modules.Resources.Queries;


namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for GetUsageByTypeQuery
/// </summary>
public class GetUsageByTypeHandler(
    ApplicationDbContext context,
    ILogger<GetUsageByTypeHandler> logger)
    : IRequestHandler<GetUsageByTypeQuery, Result<Dictionary<Guid, long>>>
{
    public async Task<Result<Dictionary<Guid, long>>> Handle(
        GetUsageByTypeQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Aggregating usage by type {UsageType} from {StartDate} to {EndDate}",
                request.UsageType, request.StartDate, request.EndDate);

            // Aggregate usage by tenant for the specified type and date range
            var usageByTenant = await context.Set<ResourceUsageRecord>()
                .Where(r => r.Type == request.UsageType)
                .Where(r => r.RecordedAt >= request.StartDate && r.RecordedAt <= request.EndDate)
                .GroupBy(r => r.TenantId)
                .Select(g => new
                {
                    TenantId = g.Key,
                    TotalUsage = g.Sum(r => r.Count)
                })
                .ToDictionaryAsync(x => x.TenantId, x => x.TotalUsage, cancellationToken);

            logger.LogInformation(
                "Aggregated usage for {TenantCount} tenants, type {UsageType}",
                usageByTenant.Count, request.UsageType);

            return Result.Success(usageByTenant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error aggregating usage by type {UsageType}", request.UsageType);
            return Result.Failure<Dictionary<Guid, long>>($"Failed to aggregate usage: {ex.Message}");
        }
    }
}
