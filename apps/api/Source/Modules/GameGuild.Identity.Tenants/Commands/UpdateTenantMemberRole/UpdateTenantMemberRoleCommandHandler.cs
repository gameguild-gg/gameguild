using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for updating a tenant member's role
/// </summary>
public sealed class UpdateTenantMemberRoleCommandHandler(
    ITenantMemberRepository memberRepository,
    ITenantRepository tenantRepository) : ICommandHandler<UpdateTenantMemberRoleCommand, UpdateTenantMemberRoleResponse>
{
    public async Task<UpdateTenantMemberRoleResponse> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (IsSystemAdminRole(request.NewRole))
        {
            return await PromoteSystemAdminInDefaultTenantAsync(request, member, cancellationToken).ConfigureAwait(false);
        }

        if (member == null)
        {
            return new UpdateTenantMemberRoleResponse { Success = false, Message = "Member not found" };
        }

        var isDefaultMembership = member.Tenant?.IsDefault == true;
        if (!member.IsActive && member.Tenant == null)
        {
            var tenant = await tenantRepository.GetByIdAsync(member.TenantId, cancellationToken).ConfigureAwait(false);
            member.Tenant = tenant;
            isDefaultMembership = tenant?.IsDefault == true;
        }

        if (isDefaultMembership)
        {
            ActivateAndAcceptDefaultMembership(member);
        }

        if (isDefaultMembership && IsSystemAdminRole(member.Role))
        {
            var activeMembers = await memberRepository.GetByTenantIdAsync(member.TenantId, false, cancellationToken).ConfigureAwait(false);
            var activeSystemAdmins = activeMembers.Count(candidate =>
                IsSystemAdminRole(candidate.Role));
            if (activeSystemAdmins <= 1)
            {
                return new UpdateTenantMemberRoleResponse
                {
                    Success = false,
                    Message = "Promote another super admin before changing the last super admin account."
                };
            }
        }

        member.UpdateRole(request.NewRole);
        await memberRepository.UpdateAsync(member, cancellationToken).ConfigureAwait(false);

        return new UpdateTenantMemberRoleResponse
        {
            Success = true,
            Message = "Member role updated successfully",
            MemberId = member.Id,
            NewRole = member.Role,
            TenantId = member.TenantId
        };
    }

    private async Task<UpdateTenantMemberRoleResponse> PromoteSystemAdminInDefaultTenantAsync(
        UpdateTenantMemberRoleCommand request,
        TenantMember? requestedMember,
        CancellationToken cancellationToken)
    {
        var membershipsResult = await memberRepository
                .GetByUserIdAsync(request.UserId, true, cancellationToken)
                .ConfigureAwait(false);
        var memberships = membershipsResult?.ToList() ?? [];

        if (requestedMember != null && memberships.All(candidate => candidate.Id != requestedMember.Id))
        {
            memberships.Add(requestedMember);
        }

        var defaultMembership = memberships.FirstOrDefault(candidate => candidate.Tenant?.IsDefault == true);
        var defaultTenant = defaultMembership?.Tenant;

        if (defaultTenant == null)
        {
            var tenants = await tenantRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            defaultTenant = tenants.FirstOrDefault(candidate => candidate.IsDefault && candidate.DeletedAt == null);
        }

        if (defaultTenant == null)
        {
            return new UpdateTenantMemberRoleResponse
            {
                Success = false,
                Message = "The default tenant could not be resolved; SystemAdmin was not assigned."
            };
        }

        defaultMembership ??= await memberRepository
            .GetByUserAndTenantIncludingDeletedAsync(request.UserId, defaultTenant.Id, cancellationToken)
            .ConfigureAwait(false);

        if (defaultMembership == null)
        {
            defaultMembership = new TenantMember
            {
                TenantId = defaultTenant.Id,
                Tenant = defaultTenant,
                UserId = request.UserId,
                Role = "SystemAdmin",
                JoinedAt = SystemClock.UtcNow,
                IsActive = true
            };
            await memberRepository.CreateAsync(defaultMembership, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            defaultMembership.Tenant ??= defaultTenant;
            ActivateAndAcceptDefaultMembership(defaultMembership);
            defaultMembership.UpdateRole("SystemAdmin");
            await memberRepository.UpdateAsync(defaultMembership, cancellationToken).ConfigureAwait(false);
        }

        foreach (var staleMembership in memberships.Where(candidate =>
                     candidate.Id != defaultMembership.Id && IsSystemAdminRole(candidate.Role)))
        {
            staleMembership.UpdateRole(TenantRole.Admin);
            await memberRepository.UpdateAsync(staleMembership, cancellationToken).ConfigureAwait(false);
        }

        return new UpdateTenantMemberRoleResponse
        {
            Success = true,
            Message = "System administrator role assigned in the default tenant.",
            MemberId = defaultMembership.Id,
            NewRole = defaultMembership.Role,
            TenantId = defaultMembership.TenantId
        };
    }

    private static void ActivateAndAcceptDefaultMembership(TenantMember member)
    {
        if (member.DeletedAt is not null)
        {
            member.Restore();
        }

        member.Activate();
        if (!string.IsNullOrWhiteSpace(member.Metadata))
        {
            member.Metadata = TenantMemberInviteMetadata
                .FromJson(member.Metadata)
                .MarkAccepted(SystemClock.UtcNow)
                .ToJson();
        }
    }

    private static bool IsSystemAdminRole(string? role) =>
        string.Equals(role, "SystemAdmin", StringComparison.OrdinalIgnoreCase);
}
