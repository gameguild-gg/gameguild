using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for adding a tenant member
/// </summary>
public sealed class AddTenantMemberHandler(
    ITenantMemberRepository repository,
    ITenantRepository tenantRepository,
    ILogger<AddTenantMemberHandler> logger) : IRequestHandler<AddTenantMemberCommand, Result<TenantMemberDto>>
{
    public async Task<Result<TenantMemberDto>> Handle(AddTenantMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant exists
            var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                return Result<TenantMemberDto>.Failure($"Tenant with ID {request.TenantId} not found");
            }

            // Check if already a member
            var existingMember = await repository.GetMemberAsync(request.UserId, request.TenantId, cancellationToken);
            if (existingMember != null)
            {
                return Result<TenantMemberDto>.Failure("User is already a member of this tenant");
            }

            // Create new member
            var member = new TenantMember
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                Role = request.Role,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var addedMember = await repository.AddMemberAsync(member, cancellationToken);

            logger.LogInformation("Added user {UserId} as member of tenant {TenantId} with role {Role}",
                request.UserId, request.TenantId, request.Role);

            var dto = new TenantMemberDto
            {
                UserId = addedMember.UserId,
                TenantId = addedMember.TenantId,
                Role = addedMember.Role,
                IsActive = addedMember.IsActive,
                JoinedAt = addedMember.JoinedAt,
                LeftAt = addedMember.LeftAt,
                LeaveReason = addedMember.LeaveReason,
                TenantName = tenant.Name,
                TenantSlug = tenant.Slug
            };

            return Result<TenantMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding member to tenant");
            return Result<TenantMemberDto>.Failure($"Error adding member: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for removing a tenant member
/// </summary>
public sealed class RemoveTenantMemberHandler(
    ITenantMemberRepository repository,
    ILogger<RemoveTenantMemberHandler> logger) : IRequestHandler<RemoveTenantMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var member = await repository.GetMemberAsync(request.UserId, request.TenantId, cancellationToken);
            if (member == null)
            {
                return Result.Failure("Member not found");
            }

            // Use domain method to leave
            member.Leave(request.LeaveReason);
            await repository.UpdateMemberAsync(member, cancellationToken);

            logger.LogInformation("Removed user {UserId} from tenant {TenantId}", request.UserId, request.TenantId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing member from tenant");
            return Result.Failure($"Error removing member: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for updating a tenant member's role
/// </summary>
public sealed class UpdateTenantMemberRoleHandler(
    ITenantMemberRepository repository,
    ILogger<UpdateTenantMemberRoleHandler> logger) : IRequestHandler<UpdateTenantMemberRoleCommand, Result<TenantMemberDto>>
{
    public async Task<Result<TenantMemberDto>> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var member = await repository.GetMemberAsync(request.UserId, request.TenantId, cancellationToken);
            if (member == null)
            {
                return Result<TenantMemberDto>.Failure("Member not found");
            }

            // Use domain method to update role
            member.UpdateRole(request.NewRole);
            var updatedMember = await repository.UpdateMemberAsync(member, cancellationToken);

            logger.LogInformation("Updated role for user {UserId} in tenant {TenantId} to {NewRole}",
                request.UserId, request.TenantId, request.NewRole);

            var dto = new TenantMemberDto
            {
                UserId = updatedMember.UserId,
                TenantId = updatedMember.TenantId,
                Role = updatedMember.Role,
                IsActive = updatedMember.IsActive,
                JoinedAt = updatedMember.JoinedAt,
                LeftAt = updatedMember.LeftAt,
                LeaveReason = updatedMember.LeaveReason
            };

            return Result<TenantMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating member role");
            return Result<TenantMemberDto>.Failure($"Error updating role: {ex.Message}");
        }
    }
}

/// <summary>
///     Handler for activating a tenant member
/// </summary>
public sealed class ActivateTenantMemberHandler(
    ITenantMemberRepository repository,
    ILogger<ActivateTenantMemberHandler> logger) : IRequestHandler<ActivateTenantMemberCommand, Result<TenantMemberDto>>
{
    public async Task<Result<TenantMemberDto>> Handle(ActivateTenantMemberCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var member = await repository.GetMemberAsync(request.UserId, request.TenantId, cancellationToken);
            if (member == null)
            {
                return Result<TenantMemberDto>.Failure("Member not found");
            }

            // Use domain method to activate
            member.Activate();
            var updatedMember = await repository.UpdateMemberAsync(member, cancellationToken);

            logger.LogInformation("Activated user {UserId} in tenant {TenantId}", request.UserId, request.TenantId);

            var dto = new TenantMemberDto
            {
                UserId = updatedMember.UserId,
                TenantId = updatedMember.TenantId,
                Role = updatedMember.Role,
                IsActive = updatedMember.IsActive,
                JoinedAt = updatedMember.JoinedAt,
                LeftAt = updatedMember.LeftAt,
                LeaveReason = updatedMember.LeaveReason
            };

            return Result<TenantMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating member");
            return Result<TenantMemberDto>.Failure($"Error activating member: {ex.Message}");
        }
    }
}
