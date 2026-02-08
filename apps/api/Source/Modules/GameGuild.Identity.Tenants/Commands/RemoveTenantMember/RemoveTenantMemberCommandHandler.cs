using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for removing a tenant member
/// </summary>
public class RemoveTenantMemberCommandHandler(ITenantRepository tenantRepository, ITenantMemberRepository memberRepository) : ICommandHandler<RemoveTenantMemberCommand, RemoveTenantMemberResponse>
{
    public async Task<RemoveTenantMemberResponse> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (member == null) { return new RemoveTenantMemberResponse { Success = false, Message = "Member not found" }; }

        await memberRepository.DeleteAsync(member.Id, cancellationToken).ConfigureAwait(false);

        // Get tenant to raise domain event
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        tenant?.AddDomainEvent(new TenantMemberRemovedEvent(request.TenantId, request.UserId, "member@email.com", "Removed by request"));

        return new RemoveTenantMemberResponse { Success = true, Message = "Member removed successfully" };
    }
}
