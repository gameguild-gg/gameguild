using GameGuild.Core;
using GameGuild.Database;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for getting tenant statistics
/// </summary>
public sealed class GetTenantStatisticsHandler(
    ApplicationDbContext dbContext,
    ILogger<GetTenantStatisticsHandler> logger) : IRequestHandler<GetTenantStatisticsQuery, Result<TenantStatisticsDto>>
{
    public async Task<Result<TenantStatisticsDto>> Handle(GetTenantStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if tenant exists
            var tenant = await dbContext.Set<Tenant>()
                .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

            if (tenant == null)
            {
                return Result<TenantStatisticsDto>.Failure($"Tenant with ID {request.TenantId} not found");
            }

            // Try to get existing statistics
            var statistics = await dbContext.Set<TenantStatistics>()
                .FirstOrDefaultAsync(s => s.TenantId == request.TenantId, cancellationToken);

            // If no statistics exist, calculate and create them
            if (statistics == null)
            {
                statistics = new TenantStatistics
                {
                    TenantId = request.TenantId,
                    Tenant = tenant
                };

                // Calculate member counts
                var memberCounts = await dbContext.Set<TenantMember>()
                    .Where(m => m.TenantId == request.TenantId)
                    .GroupBy(m => m.IsActive)
                    .Select(g => new { IsActive = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                statistics.TotalMembers = memberCounts.Sum(m => m.Count);
                statistics.ActiveMembers = memberCounts.FirstOrDefault(m => m.IsActive)?.Count ?? 0;

                // Calculate domain counts
                statistics.TotalDomains = await dbContext.Set<TenantDomain>()
                    .CountAsync(d => d.TenantId == request.TenantId, cancellationToken);

                // Calculate subscription counts
                statistics.ActiveSubscriptions = await dbContext.Set<TenantSubscription>()
                    .CountAsync(s => s.TenantId == request.TenantId && s.IsActive, cancellationToken);

                // Save new statistics
                await dbContext.Set<TenantStatistics>().AddAsync(statistics, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Created new statistics for tenant {TenantId}", request.TenantId);
            }

            var dto = new TenantStatisticsDto
            {
                TenantId = statistics.TenantId,
                TotalUsers = statistics.TotalUsers,
                ActiveUsers = statistics.ActiveUsers,
                TotalMembers = statistics.TotalMembers,
                ActiveMembers = statistics.ActiveMembers,
                TotalDomains = statistics.TotalDomains,
                StorageUsedBytes = statistics.StorageUsedBytes,
                StorageUsedMB = statistics.StorageUsedMB,
                StorageUsedGB = statistics.StorageUsedGB,
                TotalApiCalls = statistics.TotalApiCalls,
                ActiveSubscriptions = statistics.ActiveSubscriptions,
                LastUpdatedAt = statistics.LastUpdatedAt
            };

            logger.LogInformation("Retrieved statistics for tenant {TenantId}", request.TenantId);

            return Result<TenantStatisticsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving statistics for tenant {TenantId}", request.TenantId);
            return Result<TenantStatisticsDto>.Failure($"Failed to retrieve tenant statistics: {ex.Message}");
        }
    }
}
