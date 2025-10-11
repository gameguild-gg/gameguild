using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
///     Represents a user-role assignment
/// </summary>
public sealed class UserRoleAssignment
{
    /// <summary>
    ///     User's unique identifier
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Role name to assign
    /// </summary>
    public required string Role { get; init; }
}

/// <summary>
///     Command to assign roles to multiple users in bulk
/// </summary>
public sealed class BulkAssignRolesCommand : ICommand
{
    /// <summary>
    ///     Collection of user-role assignments
    /// </summary>
    public required IEnumerable<UserRoleAssignment> UserRoleAssignments { get; init; }
}

/// <summary>
///     Handler for BulkAssignRolesCommand
/// </summary>
public sealed class BulkAssignRolesCommandHandler : ICommandHandler<BulkAssignRolesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<BulkAssignRolesCommandHandler> _logger;

    public BulkAssignRolesCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        ILogger<BulkAssignRolesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(BulkAssignRolesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing bulk role assignment for {AssignmentCount} user-role assignments",
            request.UserRoleAssignments.Count()
        );

        // Verify users exist
        var userIds = request.UserRoleAssignments.Select(a => a.UserId).Distinct().ToList();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var foundUserIds = users.Select(u => u.Id).ToHashSet();

        // Validate roles exist by name
        var roleNames = request.UserRoleAssignments.Select(a => a.Role).Distinct().ToList();
        var roles = new Dictionary<string, Entities.Role>();

        foreach (var roleName in roleNames)
        {
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found in system", roleName);
                continue;
            }
            roles[roleName] = role;
        }

        // Create UserRole entities for valid assignments
        var userRolesToAssign = new List<Entities.UserRole>();

        foreach (var assignment in request.UserRoleAssignments)
        {
            if (!foundUserIds.Contains(assignment.UserId))
            {
                _logger.LogWarning(
                    "Cannot assign role {Role} to user {UserId}: User not found",
                    assignment.Role,
                    assignment.UserId
                );
                continue;
            }

            if (!roles.ContainsKey(assignment.Role))
            {
                _logger.LogWarning(
                    "Cannot assign role {Role} to user {UserId}: Role not found",
                    assignment.Role,
                    assignment.UserId
                );
                continue;
            }

            var role = roles[assignment.Role];

            // Check if assignment already exists
            var exists = await _userRoleRepository.HasRoleAsync(assignment.UserId, role.Id, cancellationToken);
            if (exists)
            {
                _logger.LogInformation(
                    "User {UserId} already has role {Role}, skipping",
                    assignment.UserId,
                    assignment.Role
                );
                continue;
            }

            userRolesToAssign.Add(new Entities.UserRole
            {
                UserId = assignment.UserId,
                RoleId = role.Id
            });
        }

        // Bulk assign roles
        if (userRolesToAssign.Count > 0)
        {
            await _userRoleRepository.AssignBulkAsync(userRolesToAssign, cancellationToken);

            _logger.LogInformation(
                "Successfully assigned {Count} role assignments",
                userRolesToAssign.Count
            );
        }
        else
        {
            _logger.LogWarning("No valid role assignments to process");
        }

        return Unit.Value;
    }
}
