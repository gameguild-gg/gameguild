using Asp.Versioning;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Queries;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Controllers;

/// <summary>
///     Controller for role management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class RolesController(ILogger<RolesController> logger, ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get all roles in the system
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="includeInactive">Whether to include inactive roles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    [Authorize] // Requires authentication
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? tenantId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting all roles");

        try
        {
            var query = new GetRolesQuery
            {
                TenantId = tenantId,
                IncludeInactive = includeInactive
            };

            var roles = await sender.Send(query, cancellationToken);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting roles");
            return StatusCode(500, "An error occurred while retrieving roles");
        }
    }

    /// <summary>
    ///     Get a specific role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting role by ID: {RoleId}", id);

        try
        {
            var query = new GetRoleByIdQuery { RoleId = id };
            var role = await sender.Send(query, cancellationToken);

            if (role == null)
            {
                return NotFound($"Role with ID '{id}' not found");
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting role by ID: {RoleId}", id);
            return StatusCode(500, "An error occurred while retrieving the role");
        }
    }

    /// <summary>
    ///     Create a new role
    /// </summary>
    /// <param name="request">Create role request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating new role: {RoleName}", request.Name);

        try
        {
            var command = new CreateRoleCommand
            {
                Name = request.Name,
                Description = request.Description,
                Permissions = request.Permissions,
                TenantId = request.TenantId
            };

            var role = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating role");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role");
            return StatusCode(500, "An error occurred while creating the role");
        }
    }

    /// <summary>
    ///     Update an existing role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="request">Update role request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating role: {RoleId}", id);

        try
        {
            var command = new UpdateRoleCommand
            {
                RoleId = id,
                Name = request.Name,
                Description = request.Description,
                Permissions = request.Permissions,
                IsActive = request.IsActive
            };

            var role = await sender.Send(command, cancellationToken);
            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating role");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating role: {RoleId}", id);
            return StatusCode(500, "An error occurred while updating the role");
        }
    }

    /// <summary>
    ///     Delete a role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting role: {RoleId}", id);

        try
        {
            var command = new DeleteRoleCommand { RoleId = id };
            await sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while deleting role");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting role: {RoleId}", id);
            return StatusCode(500, "An error occurred while deleting the role");
        }
    }

    /// <summary>
    ///     Get all roles assigned to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeExpired">Whether to include expired role assignments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("user/{userId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserRoles(Guid userId, [FromQuery] bool includeExpired = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting roles for user: {UserId}", userId);

        try
        {
            var query = new GetUserRolesQuery
            {
                UserId = userId,
                IncludeExpired = includeExpired
            };

            var roles = await sender.Send(query, cancellationToken);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting roles for user: {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user roles");
        }
    }

    /// <summary>
    ///     Assign a role to a user
    /// </summary>
    /// <param name="request">Assign role request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("assign")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleToUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

        try
        {
            var command = new AssignRoleToUserCommand
            {
                UserId = request.UserId,
                RoleId = request.RoleId,
                ExpiresAt = request.ExpiresAt
            };

            var userRole = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUserRoles), new { userId = request.UserId }, userRole);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while assigning role");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);
            return StatusCode(500, "An error occurred while assigning the role");
        }
    }

    /// <summary>
    ///     Remove a role from a user
    /// </summary>
    /// <param name="request">Remove role request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("remove")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleFromUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);

        try
        {
            var command = new RemoveRoleFromUserCommand
            {
                UserId = request.UserId,
                RoleId = request.RoleId
            };

            await sender.Send(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while removing role");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", request.RoleId, request.UserId);
            return StatusCode(500, "An error occurred while removing the role");
        }
    }
}
