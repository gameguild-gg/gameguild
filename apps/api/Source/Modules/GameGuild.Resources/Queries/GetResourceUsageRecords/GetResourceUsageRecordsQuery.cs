using GameGuild.CQRS;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to get resource usage records for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
public record GetResourceUsageRecordsQuery(Guid TenantId, ResourceUsageType? ResourceUsageType = null, DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<IEnumerable<UsageRecord>>;
