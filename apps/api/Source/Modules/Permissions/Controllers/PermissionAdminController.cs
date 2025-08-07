using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Modules.Permissions.Controllers;

/// <summary>
/// Simple admin controller for managing role templates and user permissions
/// Allows frontend admins to edit permission templates that will be automatically applied when UserCreatedEvent happens
/// </summary>
[ApiController]
[Route("api/admin/permissions")]
[Authorize] // TODO: Add admin permission check
public class PermissionAdminController : ControllerBase
{
    private readonly ISimplePermissionService _permissionService;
    private readonly ILogger<PermissionAdminController> _logger;

    public PermissionAdminController(ISimplePermissionService permissionService, ILogger<PermissionAdminController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    // ===== ROLE TEMPLATE MANAGEMENT =====

    /// <summary>
    /// Get all role templates
    /// </summary>
    [HttpGet("role-templates")]
    public async Task<ActionResult<List<RoleTemplate>>> GetRoleTemplates()
    {
        var templates = await _permissionService.GetRoleTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Get a specific role template
    /// </summary>
    [HttpGet("role-templates/{name}")]
    public async Task<ActionResult<RoleTemplate>> GetRoleTemplate(string name)
    {
        var template = await _permissionService.GetRoleTemplateAsync(name);
        if (template == null)
        {
            return NotFound($"Role template '{name}' not found");
        }
        return Ok(template);
    }

    /// <summary>
    /// Create a new role template
    /// </summary>
    [HttpPost("role-templates")]
    public async Task<ActionResult<RoleTemplate>> CreateRoleTemplate([FromBody] CreateRoleTemplateRequest request)
    {
        try
        {
            var template = await _permissionService.CreateRoleTemplateAsync(
                request.Name,
                request.Description,
                request.PermissionTemplates);

            _logger.LogInformation("Admin user {UserId} created role template '{RoleName}'", GetCurrentUserId(), request.Name);
            return CreatedAtAction(nameof(GetRoleTemplate), new { name = request.Name }, template);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing role template
    /// </summary>
    [HttpPut("role-templates/{name}")]
    public async Task<ActionResult<RoleTemplate>> UpdateRoleTemplate(string name, [FromBody] UpdateRoleTemplateRequest request)
    {
        try
        {
            var template = await _permissionService.UpdateRoleTemplateAsync(
                name,
                request.Description,
                request.PermissionTemplates);

            _logger.LogInformation("Admin user {UserId} updated role template '{RoleName}'", GetCurrentUserId(), name);
            return Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a role template
    /// </summary>
    [HttpDelete("role-templates/{name}")]
    public async Task<ActionResult> DeleteRoleTemplate(string name)
    {
        try
        {
            var deleted = await _permissionService.DeleteRoleTemplateAsync(name);
            if (!deleted)
            {
                return NotFound($"Role template '{name}' not found");
            }

            _logger.LogInformation("Admin user {UserId} deleted role template '{RoleName}'", GetCurrentUserId(), name);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ===== USER ROLE MANAGEMENT =====

    /// <summary>
    /// Get all roles assigned to a user
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    public async Task<ActionResult<List<UserRoleAssignment>>> GetUserRoles(Guid userId, [FromQuery] Guid? tenantId = null)
    {
        var roles = await _permissionService.GetUserRolesAsync(userId, tenantId);
        return Ok(roles);
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    public async Task<ActionResult> AssignRoleToUser(Guid userId, [FromBody] AdminAssignRoleRequest request)
    {
        try
        {
            await _permissionService.AssignRoleToUserAsync(userId, request.TenantId, request.RoleName, request.ExpiresAt);
            
            _logger.LogInformation("Admin user {AdminUserId} assigned role '{RoleName}' to user {UserId}", 
                GetCurrentUserId(), request.RoleName, userId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Revoke a role from a user
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleName}")]
    public async Task<ActionResult> RevokeRoleFromUser(Guid userId, string roleName, [FromQuery] Guid? tenantId = null)
    {
        await _permissionService.RevokeRoleFromUserAsync(userId, tenantId, roleName);
        
        _logger.LogInformation("Admin user {AdminUserId} revoked role '{RoleName}' from user {UserId}", 
            GetCurrentUserId(), roleName, userId);
        return NoContent();
    }

    // ===== USER PERMISSION MANAGEMENT =====

    /// <summary>
    /// Get all permissions for a user
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    public async Task<ActionResult<List<UserPermission>>> GetUserPermissions(Guid userId, [FromQuery] Guid? tenantId = null, [FromQuery] string? resourceType = null)
    {
        var permissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId, resourceType);
        return Ok(permissions);
    }

    /// <summary>
    /// Grant a specific permission to a user
    /// </summary>
    [HttpPost("users/{userId}/permissions")]
    public async Task<ActionResult> GrantPermission(Guid userId, [FromBody] GrantPermissionRequest request)
    {
        await _permissionService.GrantPermissionAsync(
            userId, 
            request.TenantId, 
            request.Action, 
            request.ResourceType, 
            request.ResourceId, 
            grantedByRole: null, // Direct permission grant
            request.ExpiresAt);

        _logger.LogInformation("Admin user {AdminUserId} granted permission '{Action}' on '{ResourceType}' to user {UserId}", 
            GetCurrentUserId(), request.Action, request.ResourceType, userId);
        return Ok();
    }

    /// <summary>
    /// Revoke a specific permission from a user
    /// </summary>
    [HttpDelete("users/{userId}/permissions")]
    public async Task<ActionResult> RevokePermission(Guid userId, [FromBody] RevokePermissionRequest request)
    {
        await _permissionService.RevokePermissionAsync(userId, request.TenantId, request.Action, request.ResourceType, request.ResourceId);
        
        _logger.LogInformation("Admin user {AdminUserId} revoked permission '{Action}' on '{ResourceType}' from user {UserId}", 
            GetCurrentUserId(), request.Action, request.ResourceType, userId);
        return NoContent();
    }

    // ===== PERMISSION CHECKING =====

    /// <summary>
    /// Check if a user has a specific permission
    /// </summary>
    [HttpGet("users/{userId}/check")]
    public async Task<ActionResult<bool>> CheckPermission(
        Guid userId, 
        [FromQuery] Guid? tenantId, 
        [FromQuery] string action, 
        [FromQuery] string resourceType, 
        [FromQuery] Guid? resourceId = null)
    {
        var hasPermission = await _permissionService.HasPermissionAsync(userId, tenantId, action, resourceType, resourceId);
        return Ok(hasPermission);
    }

    // ===== CONFIGURATION MANAGEMENT =====

    /// <summary>
    /// Get the current default role for new users
    /// </summary>
    [HttpGet("default-role")]
    public ActionResult<string> GetDefaultRole()
    {
        // TODO: This should read from configuration and allow updating it
        return Ok("TestingLabTester");
    }

    /// <summary>
    /// Update the default role for new users
    /// </summary>
    [HttpPut("default-role")]
    public ActionResult UpdateDefaultRole([FromBody] UpdateDefaultRoleRequest request)
    {
        // TODO: This should update the configuration setting
        _logger.LogInformation("Admin user {AdminUserId} updated default role to '{RoleName}'", 
            GetCurrentUserId(), request.RoleName);
        return Ok();
    }
}

// ===== REQUEST/RESPONSE MODELS =====

public class CreateRoleTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PermissionTemplate> PermissionTemplates { get; set; } = new();
}

public class UpdateRoleTemplateRequest
{
    public string Description { get; set; } = string.Empty;
    public List<PermissionTemplate> PermissionTemplates { get; set; } = new();
}

public class AdminAssignRoleRequest
{
    public Guid? TenantId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class GrantPermissionRequest
{
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class RevokePermissionRequest
{
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
}

public class UpdateDefaultRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}
