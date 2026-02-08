using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting tenant members
/// </summary>
public class GetTenantMembersQueryHandler(ITenantMemberRepository memberRepository) : IQueryHandler<GetTenantMembersQuery, GetTenantMembersResponse>
{
    public async Task<GetTenantMembersResponse> Handle(GetTenantMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await memberRepository.GetByTenantIdAsync(request.TenantId, request.IncludeInactive, cancellationToken).ConfigureAwait(false);

        // Apply role filter if specified
        var filteredMembers = members.AsEnumerable();

        if (!string.IsNullOrEmpty(request.Role)) { filteredMembers = filteredMembers.Where(m => m.Role.Equals(request.Role, StringComparison.OrdinalIgnoreCase)); }

        // Apply pagination
        var filteredMembersList = filteredMembers.ToList();
        var totalCount = filteredMembersList.Count;
        var pagedMembers = filteredMembersList.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new GetTenantMembersResponse { Members = pagedMembers, TotalCount = totalCount };
    }
}
