using GameGuild.Modules.Tenants.Commands;
using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
///     Query to get usage information for a tenant
/// </summary>
public record GetUsageQuery(
    Guid TenantId,
    ResourceType? ResourceType = null) : IRequest<Result<List<UsageTrackingDto>>>;
