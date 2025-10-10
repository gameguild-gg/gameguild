using GameGuild.Core.Messaging;
using GameGuild.Modules.Resources.DTOs;

namespace GameGuild.Modules.Resources.Queries;

/// <summary>
/// Query to get detailed usage information for a specific resource or user
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceId">Optional resource identifier filter</param>
/// <param name="UserId">Optional user identifier filter</param>
/// <param name="UsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
public record GetResourceUsageDetailsQuery(
    Guid TenantId,
    Guid? ResourceId = null,
    Guid? UserId = null,
    ResourceUsageType? UsageType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<Result<ResourceUsageDetailsResponse>>;
