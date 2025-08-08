using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Modules.TestingLab.Controllers;

/// <summary>
/// TestingLab-specific permission management controller
/// Allows admins to create role templates for TestingLab resources: sessions, locations, feedbacks, etc.
/// </summary>
[ApiController]
[Route("api/testing-lab/permissions")]
[Authorize] // TODO: Add admin permission check
public class TestingLabPermissionController : ControllerBase
{
    private readonly ISimplePermissionService _permissionService;
    private readonly ILogger<TestingLabPermissionController> _logger;

    public TestingLabPermissionController(ISimplePermissionService permissionService, ILogger<TestingLabPermissionController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    // ===== TESTING LAB ROLE TEMPLATES =====

    /// <summary>
    /// Get all TestingLab role templates
    /// </summary>
    [HttpGet("role-templates")]
    public async Task<ActionResult<List<TestingLabRoleTemplate>>> GetTestingLabRoleTemplates()
    {
        var allTemplates = await _permissionService.GetRoleTemplatesAsync();
        var testingLabTemplates = allTemplates
            .Where(t => t.Name.StartsWith("TestingLab") || t.PermissionTemplates.Any(p => IsTestingLabResource(p.ResourceType)))
            .Select(t => new TestingLabRoleTemplate
            {
                Name = t.Name,
                Description = t.Description,
                IsSystemRole = t.IsSystemRole,
                Permissions = new TestingLabPermissionsDto
                {
                    // Sessions
                    CanCreateSessions = t.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingSession"),
                    CanEditSessions = t.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingSession"),
                    CanDeleteSessions = t.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingSession"),
                    CanViewSessions = t.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingSession"),
                    
                    // Locations
                    CanCreateLocations = t.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingLocation"),
                    CanEditLocations = t.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingLocation"),
                    CanDeleteLocations = t.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingLocation"),
                    CanViewLocations = t.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingLocation"),
                    
                    // Feedback
                    CanCreateFeedback = t.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingFeedback"),
                    CanEditFeedback = t.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingFeedback"),
                    CanDeleteFeedback = t.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingFeedback"),
                    CanViewFeedback = t.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingFeedback"),
                    CanModerateFeedback = t.PermissionTemplates.Any(p => p.Action == "moderate" && p.ResourceType == "TestingFeedback"),
                    
                    // Requests
                    CanCreateRequests = t.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingRequest"),
                    CanEditRequests = t.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingRequest"),
                    CanDeleteRequests = t.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingRequest"),
                    CanViewRequests = t.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingRequest"),
                    CanApproveRequests = t.PermissionTemplates.Any(p => p.Action == "approve" && p.ResourceType == "TestingRequest"),
                    
                    // Participants
                    CanManageParticipants = t.PermissionTemplates.Any(p => p.Action == "manage" && p.ResourceType == "TestingParticipant"),
                    CanViewParticipants = t.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingParticipant"),
                }
            })
            .ToList();

        return Ok(testingLabTemplates);
    }

    /// <summary>
    /// Create a new TestingLab role template
    /// </summary>
    [HttpPost("role-templates")]
    public async Task<ActionResult<TestingLabRoleTemplate>> CreateTestingLabRoleTemplate([FromBody] CreateTestingLabRoleRequest request)
    {
        try
        {
            var permissionTemplates = BuildPermissionTemplates(request.Permissions);
            
            var template = await _permissionService.CreateRoleTemplateAsync(
                request.Name,
                request.Description,
                permissionTemplates);

            _logger.LogInformation("Admin user {UserId} created TestingLab role template '{RoleName}'", GetCurrentUserId(), request.Name);
            
            return Ok(MapToTestingLabRoleTemplate(template));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing TestingLab role template
    /// </summary>
    [HttpPut("role-templates/{name}")]
    public async Task<ActionResult<TestingLabRoleTemplate>> UpdateTestingLabRoleTemplate(string name, [FromBody] UpdateTestingLabRoleRequest request)
    {
        try
        {
            var permissionTemplates = BuildPermissionTemplates(request.Permissions);
            
            var template = await _permissionService.UpdateRoleTemplateAsync(
                name,
                request.Description,
                permissionTemplates);

            _logger.LogInformation("Admin user {UserId} updated TestingLab role template '{RoleName}'", GetCurrentUserId(), name);
            
            return Ok(MapToTestingLabRoleTemplate(template));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a TestingLab role template
    /// </summary>
    [HttpDelete("role-templates/{name}")]
    public async Task<ActionResult> DeleteTestingLabRoleTemplate(string name)
    {
        try
        {
            var deleted = await _permissionService.DeleteRoleTemplateAsync(name);
            if (!deleted)
            {
                return NotFound($"Role template '{name}' not found");
            }

            _logger.LogInformation("Admin user {UserId} deleted TestingLab role template '{RoleName}'", GetCurrentUserId(), name);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ===== USER TESTING LAB PERMISSIONS =====

    /// <summary>
    /// Get TestingLab permissions for a specific user
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserTestingLabPermissions>> GetUserTestingLabPermissions(Guid userId, [FromQuery] Guid? tenantId = null)
    {
        var userRoles = await _permissionService.GetUserRolesAsync(userId, tenantId);
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId);

        var testingLabPermissions = userPermissions.Where(p => IsTestingLabResource(p.ResourceType)).ToList();

        var result = new UserTestingLabPermissions
        {
            UserId = userId,
            TenantId = tenantId,
            AssignedRoles = userRoles.Select(r => r.RoleName).ToList(),
            Permissions = new TestingLabPermissionsDto
            {
                // Sessions
                CanCreateSessions = testingLabPermissions.Any(p => p.Action == "create" && p.ResourceType == "TestingSession"),
                CanEditSessions = testingLabPermissions.Any(p => p.Action == "edit" && p.ResourceType == "TestingSession"),
                CanDeleteSessions = testingLabPermissions.Any(p => p.Action == "delete" && p.ResourceType == "TestingSession"),
                CanViewSessions = testingLabPermissions.Any(p => p.Action == "read" && p.ResourceType == "TestingSession"),
                
                // Locations
                CanCreateLocations = testingLabPermissions.Any(p => p.Action == "create" && p.ResourceType == "TestingLocation"),
                CanEditLocations = testingLabPermissions.Any(p => p.Action == "edit" && p.ResourceType == "TestingLocation"),
                CanDeleteLocations = testingLabPermissions.Any(p => p.Action == "delete" && p.ResourceType == "TestingLocation"),
                CanViewLocations = testingLabPermissions.Any(p => p.Action == "read" && p.ResourceType == "TestingLocation"),
                
                // Feedback
                CanCreateFeedback = testingLabPermissions.Any(p => p.Action == "create" && p.ResourceType == "TestingFeedback"),
                CanEditFeedback = testingLabPermissions.Any(p => p.Action == "edit" && p.ResourceType == "TestingFeedback"),
                CanDeleteFeedback = testingLabPermissions.Any(p => p.Action == "delete" && p.ResourceType == "TestingFeedback"),
                CanViewFeedback = testingLabPermissions.Any(p => p.Action == "read" && p.ResourceType == "TestingFeedback"),
                CanModerateFeedback = testingLabPermissions.Any(p => p.Action == "moderate" && p.ResourceType == "TestingFeedback"),
                
                // Requests
                CanCreateRequests = testingLabPermissions.Any(p => p.Action == "create" && p.ResourceType == "TestingRequest"),
                CanEditRequests = testingLabPermissions.Any(p => p.Action == "edit" && p.ResourceType == "TestingRequest"),
                CanDeleteRequests = testingLabPermissions.Any(p => p.Action == "delete" && p.ResourceType == "TestingRequest"),
                CanViewRequests = testingLabPermissions.Any(p => p.Action == "read" && p.ResourceType == "TestingRequest"),
                CanApproveRequests = testingLabPermissions.Any(p => p.Action == "approve" && p.ResourceType == "TestingRequest"),
                
                // Participants
                CanManageParticipants = testingLabPermissions.Any(p => p.Action == "manage" && p.ResourceType == "TestingParticipant"),
                CanViewParticipants = testingLabPermissions.Any(p => p.Action == "read" && p.ResourceType == "TestingParticipant"),
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// Assign a TestingLab role to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    public async Task<ActionResult> AssignTestingLabRole(Guid userId, [FromBody] AssignTestingLabRoleRequest request)
    {
        try
        {
            await _permissionService.AssignRoleToUserAsync(userId, request.TenantId, request.RoleName, request.ExpiresAt);
            
            _logger.LogInformation("Admin user {AdminUserId} assigned TestingLab role '{RoleName}' to user {UserId}", 
                GetCurrentUserId(), request.RoleName, userId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Revoke a TestingLab role from a user
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleName}")]
    public async Task<ActionResult> RevokeTestingLabRole(Guid userId, string roleName, [FromQuery] Guid? tenantId = null)
    {
        await _permissionService.RevokeRoleFromUserAsync(userId, tenantId, roleName);
        
        _logger.LogInformation("Admin user {AdminUserId} revoked TestingLab role '{RoleName}' from user {UserId}", 
            GetCurrentUserId(), roleName, userId);
        return NoContent();
    }

    // ===== INDIVIDUAL RESOURCE PERMISSIONS =====

    /// <summary>
    /// Grant permission to a specific TestingLab resource
    /// </summary>
    [HttpPost("users/{userId}/resources/{resourceType}/{resourceId}")]
    public async Task<ActionResult> GrantResourcePermission(
        Guid userId, 
        string resourceType, 
        Guid resourceId, 
        [FromBody] GrantResourcePermissionRequest request)
    {
        if (!IsTestingLabResource(resourceType))
        {
            return BadRequest($"'{resourceType}' is not a valid TestingLab resource type");
        }

        await _permissionService.GrantPermissionAsync(
            userId, 
            request.TenantId, 
            request.Action, 
            resourceType, 
            resourceId, 
            grantedByRole: null, 
            request.ExpiresAt);

        _logger.LogInformation("Admin user {AdminUserId} granted permission '{Action}' on {ResourceType} {ResourceId} to user {UserId}", 
            GetCurrentUserId(), request.Action, resourceType, resourceId, userId);
        
        return Ok();
    }

    /// <summary>
    /// Revoke permission from a specific TestingLab resource
    /// </summary>
    [HttpDelete("users/{userId}/resources/{resourceType}/{resourceId}")]
    public async Task<ActionResult> RevokeResourcePermission(
        Guid userId, 
        string resourceType, 
        Guid resourceId, 
        [FromQuery] string action,
        [FromQuery] Guid? tenantId = null)
    {
        if (!IsTestingLabResource(resourceType))
        {
            return BadRequest($"'{resourceType}' is not a valid TestingLab resource type");
        }

        await _permissionService.RevokePermissionAsync(userId, tenantId, action, resourceType, resourceId);
        
        _logger.LogInformation("Admin user {AdminUserId} revoked permission '{Action}' on {ResourceType} {ResourceId} from user {UserId}", 
            GetCurrentUserId(), action, resourceType, resourceId, userId);
        
        return NoContent();
    }

    // ===== PERMISSION CHECKING =====

    /// <summary>
    /// Check if a user can perform an action on a TestingLab resource
    /// </summary>
    [HttpGet("users/{userId}/check/{resourceType}")]
    public async Task<ActionResult<bool>> CheckTestingLabPermission(
        Guid userId,
        string resourceType,
        [FromQuery] string action,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] Guid? tenantId = null)
    {
        if (!IsTestingLabResource(resourceType))
        {
            return BadRequest($"'{resourceType}' is not a valid TestingLab resource type");
        }

        var hasPermission = await _permissionService.HasPermissionAsync(userId, tenantId, action, resourceType, resourceId);
        return Ok(hasPermission);
    }

    // ===== HELPER METHODS =====

    private static bool IsTestingLabResource(string resourceType)
    {
        return resourceType switch
        {
            "TestingSession" => true,
            "TestingLocation" => true,
            "TestingFeedback" => true,
            "TestingRequest" => true,
            "TestingParticipant" => true,
            _ => false
        };
    }

    private static List<PermissionTemplate> BuildPermissionTemplates(TestingLabPermissionsDto permissions)
    {
        var templates = new List<PermissionTemplate>();

        // Sessions
        if (permissions.CanCreateSessions) templates.Add(new() { Action = "create", ResourceType = "TestingSession" });
        if (permissions.CanEditSessions) templates.Add(new() { Action = "edit", ResourceType = "TestingSession" });
        if (permissions.CanDeleteSessions) templates.Add(new() { Action = "delete", ResourceType = "TestingSession" });
        if (permissions.CanViewSessions) templates.Add(new() { Action = "read", ResourceType = "TestingSession" });

        // Locations
        if (permissions.CanCreateLocations) templates.Add(new() { Action = "create", ResourceType = "TestingLocation" });
        if (permissions.CanEditLocations) templates.Add(new() { Action = "edit", ResourceType = "TestingLocation" });
        if (permissions.CanDeleteLocations) templates.Add(new() { Action = "delete", ResourceType = "TestingLocation" });
        if (permissions.CanViewLocations) templates.Add(new() { Action = "read", ResourceType = "TestingLocation" });

        // Feedback
        if (permissions.CanCreateFeedback) templates.Add(new() { Action = "create", ResourceType = "TestingFeedback" });
        if (permissions.CanEditFeedback) templates.Add(new() { Action = "edit", ResourceType = "TestingFeedback" });
        if (permissions.CanDeleteFeedback) templates.Add(new() { Action = "delete", ResourceType = "TestingFeedback" });
        if (permissions.CanViewFeedback) templates.Add(new() { Action = "read", ResourceType = "TestingFeedback" });
        if (permissions.CanModerateFeedback) templates.Add(new() { Action = "moderate", ResourceType = "TestingFeedback" });

        // Requests
        if (permissions.CanCreateRequests) templates.Add(new() { Action = "create", ResourceType = "TestingRequest" });
        if (permissions.CanEditRequests) templates.Add(new() { Action = "edit", ResourceType = "TestingRequest" });
        if (permissions.CanDeleteRequests) templates.Add(new() { Action = "delete", ResourceType = "TestingRequest" });
        if (permissions.CanViewRequests) templates.Add(new() { Action = "read", ResourceType = "TestingRequest" });
        if (permissions.CanApproveRequests) templates.Add(new() { Action = "approve", ResourceType = "TestingRequest" });

        // Participants
        if (permissions.CanManageParticipants) templates.Add(new() { Action = "manage", ResourceType = "TestingParticipant" });
        if (permissions.CanViewParticipants) templates.Add(new() { Action = "read", ResourceType = "TestingParticipant" });

        return templates;
    }

    private static TestingLabRoleTemplate MapToTestingLabRoleTemplate(RoleTemplate template)
    {
        return new TestingLabRoleTemplate
        {
            Name = template.Name,
            Description = template.Description,
            IsSystemRole = template.IsSystemRole,
            Permissions = new TestingLabPermissionsDto
            {
                // Sessions
                CanCreateSessions = template.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingSession"),
                CanEditSessions = template.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingSession"),
                CanDeleteSessions = template.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingSession"),
                CanViewSessions = template.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingSession"),
                
                // Locations
                CanCreateLocations = template.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingLocation"),
                CanEditLocations = template.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingLocation"),
                CanDeleteLocations = template.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingLocation"),
                CanViewLocations = template.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingLocation"),
                
                // Feedback
                CanCreateFeedback = template.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingFeedback"),
                CanEditFeedback = template.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingFeedback"),
                CanDeleteFeedback = template.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingFeedback"),
                CanViewFeedback = template.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingFeedback"),
                CanModerateFeedback = template.PermissionTemplates.Any(p => p.Action == "moderate" && p.ResourceType == "TestingFeedback"),
                
                // Requests
                CanCreateRequests = template.PermissionTemplates.Any(p => p.Action == "create" && p.ResourceType == "TestingRequest"),
                CanEditRequests = template.PermissionTemplates.Any(p => p.Action == "edit" && p.ResourceType == "TestingRequest"),
                CanDeleteRequests = template.PermissionTemplates.Any(p => p.Action == "delete" && p.ResourceType == "TestingRequest"),
                CanViewRequests = template.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingRequest"),
                CanApproveRequests = template.PermissionTemplates.Any(p => p.Action == "approve" && p.ResourceType == "TestingRequest"),
                
                // Participants
                CanManageParticipants = template.PermissionTemplates.Any(p => p.Action == "manage" && p.ResourceType == "TestingParticipant"),
                CanViewParticipants = template.PermissionTemplates.Any(p => p.Action == "read" && p.ResourceType == "TestingParticipant"),
            }
        };
    }
}

// ===== TESTING LAB SPECIFIC MODELS =====

public class TestingLabPermissionsDto
{
    // Sessions
    public bool CanCreateSessions { get; set; }
    public bool CanEditSessions { get; set; }
    public bool CanDeleteSessions { get; set; }
    public bool CanViewSessions { get; set; }
    
    // Locations
    public bool CanCreateLocations { get; set; }
    public bool CanEditLocations { get; set; }
    public bool CanDeleteLocations { get; set; }
    public bool CanViewLocations { get; set; }
    
    // Feedback
    public bool CanCreateFeedback { get; set; }
    public bool CanEditFeedback { get; set; }
    public bool CanDeleteFeedback { get; set; }
    public bool CanViewFeedback { get; set; }
    public bool CanModerateFeedback { get; set; }
    
    // Requests
    public bool CanCreateRequests { get; set; }
    public bool CanEditRequests { get; set; }
    public bool CanDeleteRequests { get; set; }
    public bool CanViewRequests { get; set; }
    public bool CanApproveRequests { get; set; }
    
    // Participants
    public bool CanManageParticipants { get; set; }
    public bool CanViewParticipants { get; set; }
}

public class TestingLabRoleTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public TestingLabPermissionsDto Permissions { get; set; } = new();
}

public class UserTestingLabPermissions
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public List<string> AssignedRoles { get; set; } = new();
    public TestingLabPermissionsDto Permissions { get; set; } = new();
}

public class CreateTestingLabRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestingLabPermissionsDto Permissions { get; set; } = new();
}

public class UpdateTestingLabRoleRequest
{
    public string Description { get; set; } = string.Empty;
    public TestingLabPermissionsDto Permissions { get; set; } = new();
}

public class AssignTestingLabRoleRequest
{
    public Guid? TenantId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class GrantResourcePermissionRequest
{
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
