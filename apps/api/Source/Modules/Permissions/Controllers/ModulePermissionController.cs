using GameGuild.Common;
using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Modules.Permissions.Controllers;

/// <summary>
/// Controller for managing module-based permissions
/// Provides granular permission management for different modules like TestingLab, Projects, etc.
/// </summary>
[ApiController]
[Route("api/module-permissions")]
[Authorize]
public class ModulePermissionController : ControllerBase
{
    private readonly IModulePermissionService _modulePermissionService;
    private readonly ILogger<ModulePermissionController> _logger;

    public ModulePermissionController(
        IModulePermissionService modulePermissionService,
        ILogger<ModulePermissionController> logger)
    {
        _modulePermissionService = modulePermissionService;
        _logger = logger;
    }

    // ===== PERMISSION CHECKING =====

    /// <summary>
    /// Check if the current user has a specific permission in a module
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<bool>> HasModulePermission(
        [FromQuery] ModuleType module,
        [FromQuery] ModuleAction action,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? resourceId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.HasModulePermissionAsync(userId, tenantId, module, action, resourceId);
        return Ok(result);
    }

    /// <summary>
    /// Get all effective permissions for the current user in a module
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<List<ModulePermission>>> GetMyModulePermissions(
        [FromQuery] ModuleType module,
        [FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var permissions = await _modulePermissionService.GetUserModulePermissionsAsync(userId, tenantId, module);
        return Ok(permissions);
    }

    /// <summary>
    /// Get all effective permissions for a specific user in a module (admin only)
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    public async Task<ActionResult<List<ModulePermission>>> GetUserModulePermissions(
        Guid userId,
        [FromQuery] ModuleType module,
        [FromQuery] Guid? tenantId = null)
    {
        // TODO: Add admin permission check
        var permissions = await _modulePermissionService.GetUserModulePermissionsAsync(userId, tenantId, module);
        return Ok(permissions);
    }

    /// <summary>
    /// Get users who have a specific permission in a module
    /// </summary>
    [HttpGet("users-with-permission")]
    public async Task<ActionResult<List<Guid>>> GetUsersWithPermission(
        [FromQuery] ModuleType module,
        [FromQuery] ModuleAction action,
        [FromQuery] Guid? tenantId = null)
    {
        // TODO: Add admin permission check
        var users = await _modulePermissionService.GetUsersWithPermissionAsync(tenantId, module, action);
        return Ok(users);
    }

    // ===== ROLE MANAGEMENT =====

    /// <summary>
    /// Assign a role to a user for a specific module
    /// </summary>
    [HttpPost("assign-role")]
    public async Task<ActionResult<UserRoleAssignment>> AssignRole([FromBody] AssignRoleRequest request)
    {
        // TODO: Add admin permission check
        var assignment = await _modulePermissionService.AssignRoleAsync(
            request.UserId,
            request.TenantId,
            request.Module,
            request.RoleName,
            request.Constraints,
            request.ExpiresAt);

        return CreatedAtAction(nameof(GetUserRoles), new { userId = request.UserId, module = request.Module, tenantId = request.TenantId }, assignment);
    }

    /// <summary>
    /// Revoke a role from a user for a specific module
    /// </summary>
    [HttpDelete("revoke-role")]
    public async Task<ActionResult> RevokeRole([FromBody] RevokeRoleRequest request)
    {
        // TODO: Add admin permission check
        var result = await _modulePermissionService.RevokeRoleAsync(request.UserId, request.TenantId, request.Module, request.RoleName);
        
        if (!result)
        {
            return NotFound("Role assignment not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Get all roles assigned to a user for a specific module
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    public async Task<ActionResult<List<UserRoleAssignment>>> GetUserRoles(
        Guid userId,
        [FromQuery] ModuleType module,
        [FromQuery] Guid? tenantId = null)
    {
        var roles = await _modulePermissionService.GetUserRolesAsync(userId, tenantId, module);
        return Ok(roles);
    }

    /// <summary>
    /// Get all users with a specific role in a module
    /// </summary>
    [HttpGet("roles/{roleName}/users")]
    public async Task<ActionResult<List<UserRoleAssignment>>> GetUsersWithRole(
        string roleName,
        [FromQuery] ModuleType module,
        [FromQuery] Guid? tenantId = null)
    {
        // TODO: Add admin permission check
        var users = await _modulePermissionService.GetUsersWithRoleAsync(tenantId, module, roleName);
        return Ok(users);
    }

    // ===== TESTING LAB SPECIFIC ENDPOINTS =====

    /// <summary>
    /// Get all Testing Lab permissions for the current user
    /// </summary>
    [HttpGet("testing-lab/my-permissions")]
    public async Task<ActionResult<TestingLabPermissions>> GetMyTestingLabPermissions([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(userId, tenantId);
        return Ok(permissions);
    }

    /// <summary>
    /// Get all Testing Lab permissions for a specific user
    /// </summary>
    [HttpGet("testing-lab/users/{userId}/permissions")]
    public async Task<ActionResult<TestingLabPermissions>> GetUserTestingLabPermissions(Guid userId, [FromQuery] Guid? tenantId = null)
    {
        // TODO: Add admin permission check
        var permissions = await _modulePermissionService.GetUserTestingLabPermissionsAsync(userId, tenantId);
        return Ok(permissions);
    }

    /// <summary>
    /// Check if the current user can create testing sessions
    /// </summary>
    [HttpGet("testing-lab/can-create-sessions")]
    public async Task<ActionResult<bool>> CanCreateTestingSessions([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.CanCreateTestingSessionsAsync(userId, tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Check if the current user can delete testing sessions
    /// </summary>
    [HttpGet("testing-lab/can-delete-sessions")]
    public async Task<ActionResult<bool>> CanDeleteTestingSessions([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.CanDeleteTestingSessionsAsync(userId, tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Check if the current user can manage testers
    /// </summary>
    [HttpGet("testing-lab/can-manage-testers")]
    public async Task<ActionResult<bool>> CanManageTesters([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.CanManageTestersAsync(userId, tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Check if the current user can view testing reports
    /// </summary>
    [HttpGet("testing-lab/can-view-reports")]
    public async Task<ActionResult<bool>> CanViewTestingReports([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.CanViewTestingReportsAsync(userId, tenantId);
        return Ok(result);
    }

    /// <summary>
    /// Check if the current user can export testing data
    /// </summary>
    [HttpGet("testing-lab/can-export-data")]
    public async Task<ActionResult<bool>> CanExportTestingData([FromQuery] Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _modulePermissionService.CanExportTestingDataAsync(userId, tenantId);
        return Ok(result);
    }

    // ===== ROLE DEFINITION MANAGEMENT =====

    /// <summary>
    /// Get all role definitions for a module
    /// </summary>
    [HttpGet("modules/{module}/roles")]
    public async Task<ActionResult<List<ModuleRole>>> GetModuleRoleDefinitions(ModuleType module)
    {
        var roles = await _modulePermissionService.GetModuleRoleDefinitionsAsync(module);
        return Ok(roles);
    }

    /// <summary>
    /// Create a new role definition for a module
    /// </summary>
    [HttpPost("modules/{module}/roles")]
    public async Task<ActionResult<ModuleRole>> CreateRoleDefinition(ModuleType module, [FromBody] CreateRoleRequest request)
    {
        // TODO: Add admin permission check
        try
        {
            var role = await _modulePermissionService.CreateRoleDefinitionAsync(
                request.RoleName,
                module,
                request.Description,
                request.Permissions,
                request.Priority);

            return CreatedAtAction(nameof(GetModuleRoleDefinitions), new { module }, role);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing role definition
    /// </summary>
    [HttpPut("modules/{module}/roles/{roleName}")]
    public async Task<ActionResult<ModuleRole>> UpdateRoleDefinition(ModuleType module, string roleName, [FromBody] UpdateRoleRequest request)
    {
        // TODO: Add admin permission check
        try
        {
            var role = await _modulePermissionService.UpdateRoleDefinitionAsync(roleName, module, request.Permissions);
            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a role definition
    /// </summary>
    [HttpDelete("modules/{module}/roles/{roleName}")]
    public async Task<ActionResult> DeleteRoleDefinition(ModuleType module, string roleName)
    {
        // TODO: Add admin permission check
        try
        {
            var result = await _modulePermissionService.DeleteRoleDefinitionAsync(roleName, module);
            if (!result)
            {
                return NotFound("Role definition not found");
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ===== PRIVATE HELPERS =====

    private Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

// ===== REQUEST/RESPONSE MODELS =====

public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public ModuleType Module { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionConstraint>? Constraints { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class RevokeRoleRequest
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public ModuleType Module { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ModulePermission> Permissions { get; set; } = new();
    public int Priority { get; set; } = 0;
}

public class UpdateRoleRequest
{
    public List<ModulePermission> Permissions { get; set; } = new();
}
