using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class SetTenantMembershipStatusCommandHandler(ITenantMemberRepository memberRepository)
    : ICommandHandler<SetTenantMembershipStatusCommand, SetTenantMembershipStatusResponse>
{
    public async Task<SetTenantMembershipStatusResponse> Handle(
        SetTenantMembershipStatusCommand request,
        CancellationToken cancellationToken)
    {
        var member = await memberRepository
            .GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return new SetTenantMembershipStatusResponse
            {
                Success = false,
                NotFound = true,
                Message = "Tenant membership not found.",
            };
        }

        if (member.IsActive == request.IsActive)
        {
            return ToResponse(member, "Tenant membership already has the requested status.");
        }

        if (!request.IsActive && member.Tenant?.IsDefault == true)
        {
            return new SetTenantMembershipStatusResponse
            {
                Success = false,
                Message = "The default tenant membership must remain active.",
                MemberId = member.Id,
                IsActive = true,
            };
        }

        if (!request.IsActive && IsAdministrator(member.Role))
        {
            var members = await memberRepository
                .GetByTenantIdAsync(request.TenantId, includeInactive: true, cancellationToken)
                .ConfigureAwait(false);
            var activeAdministratorCount = members.Count(current =>
                current.IsActive && IsAdministrator(current.Role));

            if (activeAdministratorCount <= 1)
            {
                return new SetTenantMembershipStatusResponse
                {
                    Success = false,
                    Message = "The last active administrator cannot be deactivated.",
                    MemberId = member.Id,
                    IsActive = true,
                };
            }
        }

        if (request.IsActive)
        {
            member.Activate();
        }
        else
        {
            member.Deactivate(request.Reason);
        }

        await memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);
        return ToResponse(member, request.IsActive
            ? "Tenant membership activated."
            : "Tenant membership deactivated.");
    }

    private static SetTenantMembershipStatusResponse ToResponse(TenantMember member, string message) => new()
    {
        Success = true,
        Message = message,
        MemberId = member.Id,
        IsActive = member.IsActive,
    };

    private static bool IsAdministrator(string role) =>
        TenantRole.AdminRoles.Any(adminRole => adminRole.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
}
