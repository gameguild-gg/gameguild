using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for updating a tenant member's role
/// </summary>
public sealed class UpdateTenantMemberRoleCommandHandler(ITenantMemberRepository memberRepository) : ICommandHandler<UpdateTenantMemberRoleCommand, UpdateTenantMemberRoleResponse>
{
    public async Task<UpdateTenantMemberRoleResponse> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (member == null) { return new UpdateTenantMemberRoleResponse { Success = false, Message = "Member not found" }; }

        member.Role = request.NewRole;
        await memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);

        return new UpdateTenantMemberRoleResponse
        {
            Success = true,
            Message = "Member role updated successfully",
            MemberId = member.Id,
            NewRole = member.Role
        };
    }
}
