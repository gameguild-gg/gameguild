using GameGuild.CQRS;
using System.Text.Json;
using GameGuild.Messaging;
using GameGuild.Modules.Resources.Contexts;
using GameGuild.Modules.Resources.DTOs;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for GetResourceUsageDetailsQuery
/// </summary>
public class GetResourceUsageDetailsHandler(
    ApplicationDbContext context,
    ILogger<GetResourceUsageDetailsHandler> logger)
    : IRequestHandler<GetResourceUsageDetailsQuery, Result<ResourceUsageDetailsResponse>>
{
    public async Task<Result<ResourceUsageDetailsResponse>> Handle(
        GetResourceUsageDetailsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Retrieving detailed usage for tenant {TenantId}, resource: {ResourceId}, user: {UserId}",
                request.TenantId, request.ResourceId, request.UserId);

            // Build query
            var query = context.Set<ResourceUsageRecord>()
                .Where(r => r.TenantId == request.TenantId);

            // Apply filters
            if (request.ResourceId.HasValue)
            {
                query = query.Where(r => r.ResourceId == request.ResourceId.Value);
            }

            if (request.UserId.HasValue)
            {
                query = query.Where(r => r.UserId == request.UserId.Value);
            }

            if (request.UsageType.HasValue)
            {
                query = query.Where(r => r.Type == request.UsageType.Value);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(r => r.RecordedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(r => r.RecordedAt <= request.EndDate.Value);
            }

            // Get all matching records
            var records = await query
                .OrderByDescending(r => r.RecordedAt)
                .ToListAsync(cancellationToken);

            if (records.Count == 0)
            {
                return Result.Success(new ResourceUsageDetailsResponse());
            }

            // Calculate aggregations
            var totalCount = records.Sum(r => r.Count);
            var aggregation = new UsageAggregation
            {
                TotalCount = totalCount,
                AverageCount = records.Average(r => (double)r.Count),
                MinCount = records.Min(r => r.Count),
                MaxCount = records.Max(r => r.Count),
                RecordCount = records.Count,
                UniqueUsers = records.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).Distinct().Count(),
                UniqueResources = records.Where(r => r.ResourceId.HasValue).Select(r => r.ResourceId!.Value).Distinct().Count(),
                UniqueSources = records.Where(r => !string.IsNullOrEmpty(r.Source)).Select(r => r.Source!).Distinct().Count(),
                StartDate = records.Min(r => r.RecordedAt),
                EndDate = records.Max(r => r.RecordedAt)
            };

            // Breakdown by type
            var byType = records
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

            // Breakdown by source
            var bySource = records
                .Where(r => !string.IsNullOrEmpty(r.Source))
                .GroupBy(r => r.Source!)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

            // Top users
            var topUsers = records
                .Where(r => r.UserId.HasValue)
                .GroupBy(r => r.UserId!.Value)
                .Select(g => new UserUsageSummary
                {
                    UserId = g.Key,
                    TotalCount = g.Sum(r => r.Count),
                    RecordCount = g.Count(),
                    PercentageOfTotal = (double)g.Sum(r => r.Count) / totalCount * 100
                })
                .OrderByDescending(u => u.TotalCount)
                .Take(10)
                .ToList();

            // Top resources
            var topResources = records
                .Where(r => r.ResourceId.HasValue)
                .GroupBy(r => r.ResourceId!.Value)
                .Select(g => new ResourceUsageSummary
                {
                    ResourceId = g.Key,
                    TotalCount = g.Sum(r => r.Count),
                    RecordCount = g.Count(),
                    PercentageOfTotal = (double)g.Sum(r => r.Count) / totalCount * 100
                })
                .OrderByDescending(r => r.TotalCount)
                .Take(10)
                .ToList();

            // Map records with metadata parsing
            var detailItems = records.Select(r => new UsageDetailItem
            {
                Id = r.Id,
                TenantId = r.TenantId,
                Type = r.Type,
                Count = r.Count,
                Source = r.Source,
                UserId = r.UserId,
                ResourceId = r.ResourceId,
                Metadata = ParseMetadata(r.Metadata),
                RecordedAt = r.RecordedAt
            }).ToList();

            var response = new ResourceUsageDetailsResponse
            {
                Records = detailItems,
                Aggregation = aggregation,
                ByType = byType,
                BySource = bySource,
                TopUsers = topUsers,
                TopResources = topResources
            };

            logger.LogInformation(
                "Retrieved {RecordCount} detailed usage records for tenant {TenantId} (total usage: {TotalCount})",
                response.Records.Count, request.TenantId, aggregation.TotalCount);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving detailed usage for tenant {TenantId}", request.TenantId);
            return Result.Failure<ResourceUsageDetailsResponse>($"Failed to retrieve detailed usage: {ex.Message}");
        }
    }

    private static Dictionary<string, object>? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
        }
        catch
        {
            return null;
        }
    }
}
