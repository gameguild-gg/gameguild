using GameGuild.CQRS;
using GameGuild.Models;

namespace GameGuild.Resources;

/// <summary>
///     Query to get resource usage records for a tenant with optional pagination
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="PageNumber">Page number (1-based), defaults to 1</param>
/// <param name="PageSize">Number of records per page, defaults to 50 (max 200)</param>
public record GetResourceUsageRecordsQuery(
    Guid TenantId, 
    ResourceUsageType? ResourceUsageType = null, 
    DateTime? StartDate = null, 
    DateTime? EndDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedResult<UsageRecord>>;
