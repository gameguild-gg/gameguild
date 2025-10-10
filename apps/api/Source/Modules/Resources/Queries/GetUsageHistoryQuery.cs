using GameGuild.Messaging;
using GameGuild.Modules.Resources.DTOs;

namespace GameGuild.Modules.Resources.Queries;

/// <summary>
/// Query to get paginated usage history for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="UsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="PageNumber">Page number (1-based)</param>
/// <param name="PageSize">Number of records per page</param>
public record GetUsageHistoryQuery(
    Guid TenantId,
    ResourceUsageType? UsageType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<Result<UsageHistoryResponse>>;
