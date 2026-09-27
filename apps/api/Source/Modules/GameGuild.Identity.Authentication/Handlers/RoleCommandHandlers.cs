using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for CreateRoleCommand
/// </summary>
public sealed class CreateRoleCommandHandler(IRoleRepository roleRepository) : ICommandHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role with same name already exists in tenant
        var exists = await roleRepository.ExistsByNameAsync(request.Name, request.TenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException($"Role with name '{request.Name}' already exists in this tenant.");
        }

        // Create role entity
        var role = new Role(request.Name, request.Description, request.TenantId)
        {
            Permissions = JsonSerializer.Serialize(request.Permissions),
            IsActive = true
        };

        // Save to database
        var createdRole = await roleRepository.AddAsync(role, cancellationToken).ConfigureAwait(false);

        // Return DTO
        return new RoleDto
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            Permissions = JsonSerializer.Deserialize<List<string>>(createdRole.Permissions) ?? new List<string>(),
            IsActive = createdRole.IsActive,
            TenantId = createdRole.TenantId,
            CreatedAt = createdRole.CreatedAt,
            UpdatedAt = createdRole.UpdatedAt
        };
    }
}

/// <summary>
///     Handler for UpdateRoleCommand
/// </summary>
public sealed class UpdateRoleCommandHandler(IRoleRepository roleRepository) : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // Get existing role
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with ID '{request.RoleId}' not found.");
        }

        // Check if name is being changed and if new name already exists
        if (request.Name != null && request.Name != role.Name)
        {
            var exists = await roleRepository.ExistsByNameAsync(request.Name, role.TenantId, request.RoleId, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException($"Role with name '{request.Name}' already exists in this tenant.");
            }
            role.Name = request.Name;
        }

        // Update properties
        if (request.Description != null)
        {
            role.Description = request.Description;
        }

        if (request.Permissions != null)
        {
            role.Permissions = JsonSerializer.Serialize(request.Permissions);
        }

        if (request.IsActive.HasValue)
        {
            role.IsActive = request.IsActive.Value;
        }

        // Save changes
        await roleRepository.UpdateAsync(role, cancellationToken).ConfigureAwait(false);

        // Return DTO
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = JsonSerializer.Deserialize<List<string>>(role.Permissions) ?? new List<string>(),
            IsActive = role.IsActive,
            TenantId = role.TenantId,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}

/// <summary>
///     Handler for DeleteRoleCommand
/// </summary>
public sealed class DeleteRoleCommandHandler(IRoleRepository roleRepository) : ICommandHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role exists
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with ID '{request.RoleId}' not found.");
        }

        // Delete role
        await roleRepository.DeleteAsync(request.RoleId, cancellationToken).ConfigureAwait(false);

        return true;
    }
}

/// <summary>
///     Handler for AssignRoleToUserCommand
/// </summary>
public sealed class AssignRoleToUserCommandHandler(IRoleRepository roleRepository) : ICommandHandler<AssignRoleToUserCommand, UserRoleDto>
{
    public async Task<UserRoleDto> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        // Check if role exists
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken).ConfigureAwait(false);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with ID '{request.RoleId}' not found.");
        }

        // Check if user already has this role
        var hasRole = await roleRepository.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken).ConfigureAwait(false);
        if (hasRole)
        {
            throw new InvalidOperationException($"User already has role '{role.Name}'.");
        }

        // Create user-role assignment
        var userRole = new UserRole(request.UserId, request.RoleId, request.AssignedBy)
        {
            ExpiresAt = request.ExpiresAt
        };

        // Save to database
        var createdUserRole = await roleRepository.AssignRoleToUserAsync(userRole, cancellationToken).ConfigureAwait(false);

        // Return DTO
        return new UserRoleDto
        {
            Id = createdUserRole.Id,
            UserId = createdUserRole.UserId,
            RoleId = createdUserRole.RoleId,
            Role = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = JsonSerializer.Deserialize<List<string>>(role.Permissions) ?? new List<string>(),
                IsActive = role.IsActive,
                TenantId = role.TenantId,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            },
            AssignedBy = createdUserRole.AssignedBy,
            AssignedAt = createdUserRole.AssignedAt,
            ExpiresAt = createdUserRole.ExpiresAt,
            IsExpired = createdUserRole.IsExpired()
        };
    }
}

/// <summary>
///     Handles bounded, atomic bulk assignment of one active role to multiple users.
/// </summary>
public sealed class BulkAssignRolesCommandHandler(
    IRoleRepository roleRepository,
    IActorContextAccessor actorContextAccessor
) : ICommandHandler<BulkAssignRolesCommand, BulkRoleAssignmentResult>
{
    private const int MaximumUsersPerRequest = 500;
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<BulkRoleAssignmentResult> Handle(BulkAssignRolesCommand request, CancellationToken cancellationToken)
    {
        if (!Actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (!Actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("Bulk role assignment requires system administration");
        }

        if (request.RoleId == Guid.Empty)
        {
            throw new ArgumentException("A role ID is required", nameof(request));
        }

        if (request.UserIds is null || request.UserIds.Count is 0 or > MaximumUsersPerRequest)
        {
            throw new ArgumentException($"Between 1 and {MaximumUsersPerRequest} users must be specified", nameof(request));
        }

        if (request.UserIds.Any(userId => userId == Guid.Empty))
        {
            throw new ArgumentException("User IDs cannot be empty", nameof(request));
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= SystemClock.UtcNow)
        {
            throw new ArgumentException("Role expiration must be in the future", nameof(request));
        }

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken).ConfigureAwait(false);
        if (role is null || !role.IsActive)
        {
            throw new InvalidOperationException($"Active role with ID '{request.RoleId}' was not found.");
        }

        var uniqueUserIds = request.UserIds.Distinct().ToArray();
        var outcomes = await roleRepository.BulkAssignRoleToUsersAsync(
                role.Id,
                uniqueUserIds,
                Actor.SubjectIdAsGuid,
                request.ExpiresAt,
                cancellationToken)
            .ConfigureAwait(false);

        return new BulkRoleAssignmentResult
        {
            RoleId = role.Id,
            TotalRequested = uniqueUserIds.Length,
            DuplicateUserIds = request.UserIds.Count - uniqueUserIds.Length,
            Assigned = outcomes.Count(result => result.Status == BulkRoleAssignmentStatus.Assigned),
            Reactivated = outcomes.Count(result => result.Status == BulkRoleAssignmentStatus.Reactivated),
            AlreadyAssigned = outcomes.Count(result => result.Status == BulkRoleAssignmentStatus.AlreadyAssigned),
            Users = outcomes
        };
    }
}

/// <summary>
///     Handler for RemoveRoleFromUserCommand
/// </summary>
public sealed class RemoveRoleFromUserCommandHandler(IRoleRepository roleRepository) : ICommandHandler<RemoveRoleFromUserCommand, bool>
{
    public async Task<bool> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        // Check if user has this role
        var hasRole = await roleRepository.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken).ConfigureAwait(false);
        if (!hasRole)
        {
            throw new InvalidOperationException($"User does not have this role.");
        }

        // Remove role from user
        await roleRepository.RemoveRoleFromUserAsync(request.UserId, request.RoleId, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
