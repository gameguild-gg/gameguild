using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get tenant members
/// </summary>
public record GetTenantMembersQuery(Guid TenantId, int PageNumber = 1, int PageSize = 20, string? Role = null, bool IncludeInactive = false) : IQuery<GetTenantMembersResponse>;
