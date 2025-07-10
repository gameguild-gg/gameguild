using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Tenants.Commands;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Tenants.Events;
using GameGuild.Modules.Tenants.Queries;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants.Handlers;

/// <summary>
/// Handler for searching tenants with advanced filtering
/// </summary>
public class SearchTenantsHandler(
    ApplicationDbContext context,
    ILogger<SearchTenantsHandler> logger
) : IQueryHandler<SearchTenantsQuery, Common.Result<IEnumerable<Tenant>>>
{
    public async Task<Common.Result<IEnumerable<Tenant>>> Handle(SearchTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = context.Resources.OfType<Tenant>().AsQueryable();

            // Filter by deleted status
            if (!request.IncludeDeleted)
            {
                query = query.Where(t => t.DeletedAt == null);
            }

            // Filter by active status
            if (request.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == request.IsActive.Value);
            }

            // Search term filtering
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLowerInvariant();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                    t.Slug.ToLower().Contains(searchTerm)
                );
            }

            // Apply sorting
            query = request.SortBy switch
            {
                TenantSortField.Name => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                TenantSortField.Description => request.SortDescending ? query.OrderByDescending(t => t.Description) : query.OrderBy(t => t.Description),
                TenantSortField.IsActive => request.SortDescending ? query.OrderByDescending(t => t.IsActive) : query.OrderBy(t => t.IsActive),
                TenantSortField.Slug => request.SortDescending ? query.OrderByDescending(t => t.Slug) : query.OrderBy(t => t.Slug),
                TenantSortField.CreatedAt => request.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                TenantSortField.UpdatedAt => request.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
                _ => query.OrderBy(t => t.Name)
            };

            // Apply pagination
            if (request.Offset.HasValue)
            {
                query = query.Skip(request.Offset.Value);
            }

            if (request.Limit.HasValue)
            {
                query = query.Take(request.Limit.Value);
            }

            var tenants = await query.ToListAsync(cancellationToken);

            logger.LogInformation("Search returned {Count} tenants with term '{SearchTerm}'", tenants.Count, request.SearchTerm ?? "");
            return Result.Success<IEnumerable<Tenant>>(tenants);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching tenants with term '{SearchTerm}'", request.SearchTerm);
            return Result.Failure<IEnumerable<Tenant>>(
                Common.Error.Failure("Tenant.SearchFailed", "Failed to search tenants")
            );
        }
    }
}

/// <summary>
/// Handler for getting tenant statistics
/// </summary>
public class GetTenantStatisticsHandler(
    ApplicationDbContext context,
    ILogger<GetTenantStatisticsHandler> logger
) : IQueryHandler<GetTenantStatisticsQuery, Common.Result<TenantStatistics>>
{
    public async Task<Common.Result<TenantStatistics>> Handle(GetTenantStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var allTenants = context.Resources.OfType<Tenant>();
            var activeTenants = allTenants.Where(t => t.DeletedAt == null);

            var statistics = new TenantStatistics
            {
                TotalTenants = await activeTenants.CountAsync(cancellationToken),
                ActiveTenants = await activeTenants.CountAsync(t => t.IsActive, cancellationToken),
                InactiveTenants = await activeTenants.CountAsync(t => !t.IsActive, cancellationToken),
                DeletedTenants = await allTenants.CountAsync(t => t.DeletedAt != null, cancellationToken),
                OldestTenantCreatedAt = await activeTenants.MinAsync(t => (DateTime?)t.CreatedAt, cancellationToken),
                NewestTenantCreatedAt = await activeTenants.MaxAsync(t => (DateTime?)t.CreatedAt, cancellationToken)
            };

            logger.LogInformation("Generated tenant statistics: {Total} total, {Active} active, {Inactive} inactive, {Deleted} deleted", 
                statistics.TotalTenants, statistics.ActiveTenants, statistics.InactiveTenants, statistics.DeletedTenants);

            return Result.Success(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating tenant statistics");
            return Result.Failure<TenantStatistics>(
                Common.Error.Failure("Tenant.StatisticsFailed", "Failed to generate tenant statistics")
            );
        }
    }
}

/// <summary>
/// Handler for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsHandler(
    ApplicationDbContext context,
    ILogger<BulkDeleteTenantsHandler> logger,
    IDomainEventPublisher eventPublisher
) : ICommandHandler<BulkDeleteTenantsCommand, Common.Result<int>>
{
    public async Task<Common.Result<int>> Handle(BulkDeleteTenantsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantIds = request.TenantIds.ToList();
            if (!tenantIds.Any())
            {
                return Result.Success(0);
            }

            var tenants = await context.Resources.OfType<Tenant>()
                .Where(t => tenantIds.Contains(t.Id) && t.DeletedAt == null)
                .ToListAsync(cancellationToken);

            if (!tenants.Any())
            {
                return Result.Success(0);
            }

            foreach (var tenant in tenants)
            {
                tenant.SoftDelete();
                
                // Publish domain event for each deleted tenant
                await eventPublisher.PublishAsync(
                    new TenantDeletedEvent(tenant.Id, tenant.Name),
                    cancellationToken
                );
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Bulk deleted {Count} tenants", tenants.Count);
            return Result.Success(tenants.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk deleting tenants");
            return Result.Failure<int>(
                Common.Error.Failure("Tenant.BulkDeleteFailed", "Failed to bulk delete tenants")
            );
        }
    }
}

/// <summary>
/// Handler for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsHandler(
    ApplicationDbContext context,
    ILogger<BulkRestoreTenantsHandler> logger,
    IDomainEventPublisher eventPublisher
) : ICommandHandler<BulkRestoreTenantsCommand, Common.Result<int>>
{
    public async Task<Common.Result<int>> Handle(BulkRestoreTenantsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantIds = request.TenantIds.ToList();
            if (!tenantIds.Any())
            {
                return Result.Success(0);
            }

            var tenants = await context.Resources.OfType<Tenant>()
                .Where(t => tenantIds.Contains(t.Id) && t.DeletedAt != null)
                .ToListAsync(cancellationToken);

            if (!tenants.Any())
            {
                return Result.Success(0);
            }

            foreach (var tenant in tenants)
            {
                tenant.Restore();
                
                // Publish domain event for each restored tenant
                await eventPublisher.PublishAsync(
                    new TenantRestoredEvent(tenant.Id, tenant.Name),
                    cancellationToken
                );
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Bulk restored {Count} tenants", tenants.Count);
            return Result.Success(tenants.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk restoring tenants");
            return Result.Failure<int>(
                Common.Error.Failure("Tenant.BulkRestoreFailed", "Failed to bulk restore tenants")
            );
        }
    }
}
