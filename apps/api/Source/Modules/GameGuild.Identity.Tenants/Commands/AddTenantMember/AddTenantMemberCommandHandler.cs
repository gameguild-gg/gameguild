using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for adding a tenant member
/// </summary>
public class AddTenantMemberCommandHandler(ITenantRepository tenantRepository, ITenantMemberRepository memberRepository) : ICommandHandler<AddTenantMemberCommand, AddTenantMemberResponse>
{
    public async Task<AddTenantMemberResponse> Handle(AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null) { return new AddTenantMemberResponse { Success = false, Message = $"Tenant with ID {request.TenantId} not found" }; }

        // Check if member already exists
        var existingMember = await memberRepository.GetByUserAndTenantAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (existingMember != null) { return new AddTenantMemberResponse { Success = false, Message = "User is already a member of this tenant" }; }

        // Create new member
        var member = new TenantMember { TenantId = request.TenantId, UserId = request.UserId, Role = request.Role, JoinedAt = DateTime.UtcNow, IsActive = true };

        var createdMember = await memberRepository.CreateAsync(member, cancellationToken).ConfigureAwait(false);

        // Raise domain event
        tenant.AddDomainEvent(new TenantMemberAddedEvent(request.TenantId, request.UserId, request.InvitedByEmail ?? "unknown@email.com", request.Role));

        return new AddTenantMemberResponse { Success = true, Message = "Member added successfully", MemberId = createdMember.Id };
    }
}
