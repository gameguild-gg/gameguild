using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for removing a tenant member
/// </summary>
public sealed class RemoveTenantMemberCommandHandler(ITenantRepository tenantRepository, ITenantMemberRepository memberRepository) : ICommandHandler<RemoveTenantMemberCommand, RemoveTenantMemberResponse>
{
    public async Task<RemoveTenantMemberResponse> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (member == null) { return new RemoveTenantMemberResponse { Success = false, Message = "Member not found" }; }

        var tenant = member.Tenant ?? await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
        {
            return new RemoveTenantMemberResponse { Success = false, Message = "Tenant not found; membership was not removed" };
        }

        if (tenant.IsDefault)
        {
            return new RemoveTenantMemberResponse
            {
                Success = false,
                Message = "The default tenant membership cannot be removed."
            };
        }

        await memberRepository.DeleteAsync(member.Id, cancellationToken).ConfigureAwait(false);

        tenant.AddDomainEvent(new TenantMemberRemovedEvent(request.TenantId, request.UserId, "member@email.com", "Removed by request"));

        return new RemoveTenantMemberResponse { Success = true, Message = "Member removed successfully" };
    }
}
