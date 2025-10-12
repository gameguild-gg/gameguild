using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get tenant members with pagination </summary>
public class GetTenantMembersPagedQuery : IQuery<Result<PagedResult<TenantMember>>>
{
    public Guid TenantId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool IncludeInactive { get; init; } = false;
    public string? Role { get; init; }
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; } = "JoinedAt";
    public bool SortDescending { get; init; } = true;
}