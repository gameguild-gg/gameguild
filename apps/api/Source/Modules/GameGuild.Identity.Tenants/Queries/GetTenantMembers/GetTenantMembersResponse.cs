
namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response for getting tenant members
/// </summary>
public record GetTenantMembersResponse
{
    public IEnumerable<TenantMember> Members { get; init; } = [];

    public int TotalCount { get; init; }
}
