using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;
using GameGuild.Tenants.Events;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for removing a tenant member
/// </summary>
public class RemoveTenantMemberCommandHandler(ITenantRepository tenantRepository, ITenantMemberRepository memberRepository) : ICommandHandler<RemoveTenantMemberCommand, RemoveTenantMemberResponse>
{
    public async Task<RemoveTenantMemberResponse> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken);

        if (member == null) { return new RemoveTenantMemberResponse { Success = false, Message = "Member not found" }; }

        await memberRepository.DeleteAsync(member.Id, cancellationToken);

        // Get tenant to raise domain event
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        tenant?.AddDomainEvent(new TenantMemberRemovedEvent(request.TenantId, request.UserId, "member@email.com", "Removed by request"));

        return new RemoveTenantMemberResponse { Success = true, Message = "Member removed successfully" };
    }
}
