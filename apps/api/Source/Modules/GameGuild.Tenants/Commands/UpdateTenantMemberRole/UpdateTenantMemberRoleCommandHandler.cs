using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for updating a tenant member's role
/// </summary>
public class UpdateTenantMemberRoleCommandHandler(ITenantMemberRepository memberRepository) : ICommandHandler<UpdateTenantMemberRoleCommand, UpdateTenantMemberRoleResponse>
{
    public async Task<UpdateTenantMemberRoleResponse> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken);

        if (member == null) { return new UpdateTenantMemberRoleResponse { Success = false, Message = "Member not found" }; }

        member.Role = request.NewRole;
        await memberRepository.UpdateAsync(member, cancellationToken);

        return new UpdateTenantMemberRoleResponse { Success = true, Message = "Member role updated successfully" };
    }
}
