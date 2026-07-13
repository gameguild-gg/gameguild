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

        var demotesSystemAdmin = string.Equals(member.Role, "SystemAdmin", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(request.NewRole, "SystemAdmin", StringComparison.OrdinalIgnoreCase);
        if (demotesSystemAdmin)
        {
            var activeMembers = await memberRepository.GetByTenantIdAsync(request.TenantId, false, cancellationToken).ConfigureAwait(false);
            var activeSystemAdmins = activeMembers.Count(candidate =>
                string.Equals(candidate.Role, "SystemAdmin", StringComparison.OrdinalIgnoreCase));
            if (activeSystemAdmins <= 1)
            {
                return new UpdateTenantMemberRoleResponse
                {
                    Success = false,
                    Message = "Promote another super admin before changing the last super admin account."
                };
            }
        }

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
