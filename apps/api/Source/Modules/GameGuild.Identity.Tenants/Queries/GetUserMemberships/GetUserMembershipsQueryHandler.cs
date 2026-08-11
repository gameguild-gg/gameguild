using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting all tenant memberships for a user.
/// </summary>
public sealed class GetUserMembershipsQueryHandler : IQueryHandler<GetUserMembershipsQuery, GetUserMembershipsResponse>
{
    private readonly ITenantMemberRepository _memberRepository;

    public GetUserMembershipsQueryHandler(ITenantMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<GetUserMembershipsResponse> Handle(GetUserMembershipsQuery request, CancellationToken cancellationToken)
    {
        var memberships = await _memberRepository
            .GetByUserIdAsync(request.UserId, request.IncludeInactive, cancellationToken)
            .ConfigureAwait(false);

        var dtos = memberships.Select(m =>
        {
            var invite = TenantMemberInviteMetadata.FromJson(m.Metadata);

            return new UserMembershipDto
            {
                MembershipId = m.Id,
                TenantId = m.TenantId,
                TenantName = m.Tenant?.Name ?? string.Empty,
                TenantSlug = m.Tenant?.Slug ?? string.Empty,
                TenantIsActive = m.Tenant?.IsActive ?? false,
                TenantIsDefault = m.Tenant?.IsDefault ?? false,
                TenantDescription = m.Tenant?.Description,
                Role = m.Role,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt,
                InviteStatus = invite.InviteStatus,
                InvitedByEmail = invite.InvitedByEmail,
                InviteeEmail = invite.InviteeEmail,
                InviteeName = invite.InviteeName,
                InvitedAt = invite.InvitedAt,
                LastInviteSentAt = invite.LastSentAt,
                AcceptedAt = invite.AcceptedAt,
                CancelledAt = invite.CancelledAt,
                InviteResendCount = invite.ResendCount
            };
        }).ToList();

        return new GetUserMembershipsResponse
        {
            Memberships = dtos,
            TotalCount = dtos.Count
        };
    }
}
