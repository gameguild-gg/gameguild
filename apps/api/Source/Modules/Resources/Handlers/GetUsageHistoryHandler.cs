using GameGuild.Database;
using GameGuild.CQRS;
using GameGuild.Modules.Resources.Contexts;
using GameGuild.Modules.Resources.DTOs;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for GetUsageHistoryQuery
/// </summary>
public class GetUsageHistoryHandler(
    ApplicationDbContext context,
    ILogger<GetUsageHistoryHandler> logger)
    : IRequestHandler<GetUsageHistoryQuery, Result<UsageHistoryResponse>>
{
    public async Task<Result<UsageHistoryResponse>> Handle(
        GetUsageHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Retrieving usage history for tenant {TenantId}, type: {UsageType}, date range: {StartDate} to {EndDate}",
                request.TenantId, request.UsageType, request.StartDate, request.EndDate);

            // Build query
            var query = context.Set<ResourceUsageRecord>()
                .Where(r => r.TenantId == request.TenantId);

            // Apply filters
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

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and ordering
            var records = await query
                .OrderByDescending(r => r.RecordedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Calculate cumulative usage and trends
            var historyItems = new List<UsageHistoryItem>();
            long cumulativeUsage = 0;
            long? previousCount = null;

            foreach (var record in records.OrderBy(r => r.RecordedAt))
            {
                cumulativeUsage += record.Count;

                double? percentageChange = null;
                if (previousCount.HasValue && previousCount.Value > 0)
                {
                    percentageChange = ((double)(record.Count - previousCount.Value) / previousCount.Value) * 100;
                }

                historyItems.Add(new UsageHistoryItem
                {
                    Id = record.Id,
                    TenantId = record.TenantId,
                    Type = record.Type,
                    Count = record.Count,
                    Source = record.Source,
                    UserId = record.UserId,
                    ResourceId = record.ResourceId,
                    Metadata = record.Metadata,
                    RecordedAt = record.RecordedAt,
                    CumulativeUsage = cumulativeUsage,
                    PercentageChange = percentageChange
                });

                previousCount = record.Count;
            }

            // Reverse to show most recent first
            historyItems.Reverse();

            var response = new UsageHistoryResponse
            {
                Records = historyItems,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            logger.LogInformation(
                "Retrieved {Count} usage history records for tenant {TenantId} (page {Page} of {TotalPages})",
                response.Records.Count, request.TenantId, response.PageNumber, response.TotalPages);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving usage history for tenant {TenantId}", request.TenantId);
            return Result.Failure<UsageHistoryResponse>($"Failed to retrieve usage history: {ex.Message}");
        }
    }
}
