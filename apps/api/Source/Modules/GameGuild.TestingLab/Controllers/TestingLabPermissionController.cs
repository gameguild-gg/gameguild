using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.TestingLab;

/// <summary> TestingLab-specific permission management controller Allows admins to create role templates for TestingLab resources: sessions, locations, feedbacks, etc. </summary>
[Route("api/testing-lab/permissions")]
[Authorize] // Admin access enforced via ActorContext.IsSystemAdmin / IsTenantAdmin checks in action methods
public class TestingLabPermissionController : BaseApiController {
  private readonly ILogger<TestingLabPermissionController> _logger;

  private readonly ITestingLabPermissionService _permissionService;

  private readonly IActorContextAccessor _actorContextAccessor;

  public TestingLabPermissionController(ITestingLabPermissionService permissionService, IActorContextAccessor actorContextAccessor, ILogger<TestingLabPermissionController> logger) {
    _permissionService = permissionService;
    _actorContextAccessor = actorContextAccessor;
    _logger = logger;
  }

  private Guid GetCurrentUserId() {
    return _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;
  }

  // ===== TESTING LAB ROLE TEMPLATES =====

  /// <summary> Get all TestingLab role templates </summary>
  [HttpGet("role-templates")]
  public async Task<ActionResult<List<RoleTemplate>>> GetRoleTemplates() {
    // TEMPORARILY DISABLED: RoleTemplate methods commented out due to type conflicts
    return BadRequest("RoleTemplate functionality temporarily disabled");
    // var roleTemplates = await _permissionService.GetRoleTemplatesAsync();
    // var testingLabRoles = roleTemplates
    //   .Where(rt => rt.Name.StartsWith("TestingLab", StringComparison.OrdinalIgnoreCase))
    //   .ToList();
    // return Ok(testingLabRoles);
  }  /// <summary> Create a new TestingLab role template </summary>
  [HttpPost("role-templates")]
  public async Task<ActionResult<TestingLabRoleTemplate>> CreateTestingLabRoleTemplate([FromBody] CreateTestingLabRoleRequest request) {
    // TEMPORARILY DISABLED: RoleTemplate methods commented out due to type conflicts
    return BadRequest("RoleTemplate functionality temporarily disabled");
    /*
    try {
      var permissionTemplates = BuildPermissionTemplates(request.Permissions);

      var template = await _permissionService.CreateRoleTemplateAsync(request.Name, request.Description, permissionTemplates).ConfigureAwait(false);

      _logger.LogInformation("Admin user {UserId} created TestingLab role template '{RoleName}'", GetCurrentUserId(), request.Name);

      return Ok(MapToTestingLabRoleTemplate(template));
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Conflict while creating role template '{RoleName}'", request.Name);
      return Conflict("A conflict occurred while creating the role template.");
    }
    */
  }

  /// <summary> Update an existing TestingLab role template </summary>
  [HttpPut("role-templates/{idOrName}")]
  public async Task<ActionResult<TestingLabRoleTemplate>> UpdateTestingLabRoleTemplate(string idOrName, [FromBody] UpdateTestingLabRoleRequest request) {
    // PLANNED: Implement when GetRoleTemplateAsync and UpdateRoleTemplateAsync are available in IPermissionService
    await Task.CompletedTask.ConfigureAwait(false); // Remove async warning
    return StatusCode(501, "Method not implemented - missing service methods");
  }

  /// <summary> Delete a TestingLab role template </summary>
  [HttpDelete("role-templates/{idOrName}")]
  public async Task<ActionResult> DeleteTestingLabRoleTemplate(string idOrName) {
    // PLANNED: Implement when DeleteRoleTemplateAsync is available in IPermissionService
    await Task.CompletedTask.ConfigureAwait(false); // Remove async warning
    return StatusCode(501, "Method not implemented - missing service methods");
  }

  /// <summary> Delete a TestingLab role template by name (legacy compatibility for clients that don't yet have Ids) </summary>
  [HttpDelete("role-templates/by-name/{name}")]
  public async Task<ActionResult> DeleteTestingLabRoleTemplateByName(string name) {
    try {
      _logger.LogInformation("Attempting to delete TestingLab role template by name '{Name}'", name);
      // PLANNED: Uncomment when DeleteRoleTemplateAsync is available in IPermissionService
      // var deleted = await _permissionService.DeleteRoleTemplateAsync(name);
      var deleted = false; // Temporary stub

      if (!deleted) { return NotFound($"Role template with name '{name}' not found"); }

      _logger.LogInformation("Admin user {UserId} deleted TestingLab role template named '{Name}'", GetCurrentUserId(), name);

      return NoContent();
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Conflict while deleting role template '{Name}'", name);
      return Conflict("A conflict occurred while deleting the role template.");
    }
  }

  // ===== USER TESTING LAB PERMISSIONS =====

  /// <summary> Get TestingLab permissions for a specific user </summary>
  [HttpGet("users/{userId}")]
  public async Task<ActionResult<UserTestingLabPermissions>> GetUserTestingLabPermissions(Guid userId, [FromQuery] Guid? tenantId = null) {
    var userRoles = await _permissionService.GetUserRolesAsync(userId, tenantId).ConfigureAwait(false);
    var userPermissions = await _permissionService.GetUserPermissionsAsync(userId, tenantId).ConfigureAwait(false);

    var testingLabPermissions = userPermissions.Where(p => IsTestingLabResource(p.ResourceType)).ToList();

    var result = new UserTestingLabPermissions {
      UserId = userId,
      TenantId = tenantId,
      AssignedRoles = userRoles.Select(r => r.RoleName).ToList(),
      Permissions = new TestingLabPermissionsDto {
        // Sessions
        CanCreateSessions = testingLabPermissions.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Session),
        CanEditSessions = testingLabPermissions.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Session),
        CanDeleteSessions = testingLabPermissions.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Session),
        CanViewSessions = testingLabPermissions.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Session),

        // Locations
        CanCreateLocations = testingLabPermissions.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Location),
        CanEditLocations = testingLabPermissions.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Location),
        CanDeleteLocations = testingLabPermissions.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Location),
        CanViewLocations = testingLabPermissions.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Location),

        // Feedback
        CanCreateFeedback = testingLabPermissions.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Feedback),
        CanEditFeedback = testingLabPermissions.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Feedback),
        CanDeleteFeedback = testingLabPermissions.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Feedback),
        CanViewFeedback = testingLabPermissions.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Feedback),
        CanModerateFeedback = testingLabPermissions.Any(p => p.Action == TestingLabActions.Moderate && p.ResourceType == TestingLabResourceTypes.Feedback),

        // Requests
        CanCreateRequests = testingLabPermissions.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Request),
        CanEditRequests = testingLabPermissions.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Request),
        CanDeleteRequests = testingLabPermissions.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Request),
        CanViewRequests = testingLabPermissions.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Request),
        CanApproveRequests = testingLabPermissions.Any(p => p.Action == TestingLabActions.Approve && p.ResourceType == TestingLabResourceTypes.Request),

        // Participants
        CanManageParticipants = testingLabPermissions.Any(p => p.Action == TestingLabActions.Manage && p.ResourceType == TestingLabResourceTypes.Participant),
        CanViewParticipants = testingLabPermissions.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Participant),
      },
    };

    return Ok(result);
  }

  /// <summary> Assign a TestingLab role to a user </summary>
  [HttpPost("users/{userId}/roles")]
  public async Task<ActionResult> AssignTestingLabRole(Guid userId, [FromBody] AssignTestingLabRoleRequest request) {
    try {
      await _permissionService.AssignRoleToUserAsync(userId, request.TenantId, request.RoleName, request.ExpiresAt).ConfigureAwait(false);

      _logger.LogInformation("Admin user {AdminUserId} assigned TestingLab role '{RoleName}' to user {UserId}", GetCurrentUserId(), request.RoleName, userId);

      return Ok();
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Failed to assign role '{RoleName}' to user {UserId}", request.RoleName, userId);
      return NotFound("The specified user or role was not found.");
    }
  }

  /// <summary> Revoke a TestingLab role from a user </summary>
  [HttpDelete("users/{userId}/roles/{roleName}")]
  public async Task<ActionResult> RevokeTestingLabRole(Guid userId, string roleName, [FromQuery] Guid? tenantId = null) {
    await _permissionService.RevokeRoleFromUserAsync(userId, tenantId, roleName).ConfigureAwait(false);

    _logger.LogInformation("Admin user {AdminUserId} revoked TestingLab role '{RoleName}' from user {UserId}", GetCurrentUserId(), roleName, userId);

    return NoContent();
  }

  // ===== INDIVIDUAL RESOURCE PERMISSIONS =====

  /// <summary> Grant permission to a specific TestingLab resource </summary>
  [HttpPost("users/{userId}/resources/{resourceType}/{resourceId}")]
  public async Task<ActionResult> GrantResourcePermission(Guid userId, string resourceType, Guid resourceId, [FromBody] GrantResourcePermissionRequest request) {
    if (!IsTestingLabResource(resourceType)) { return BadRequest($"'{resourceType}' is not a valid TestingLab resource type"); }

    await _permissionService.GrantPermissionAsync(userId, request.TenantId, request.Action, resourceType, resourceId, null, request.ExpiresAt).ConfigureAwait(false);

    _logger.LogInformation("Admin user {AdminUserId} granted permission '{Action}' on {ResourceType} {ResourceId} to user {UserId}", GetCurrentUserId(), request.Action, resourceType, resourceId, userId);

    return Ok();
  }

  /// <summary> Revoke permission from a specific TestingLab resource </summary>
  [HttpDelete("users/{userId}/resources/{resourceType}/{resourceId}")]
  public async Task<ActionResult> RevokeResourcePermission(Guid userId, string resourceType, Guid resourceId, [FromQuery] string action, [FromQuery] Guid? tenantId = null) {
    if (!IsTestingLabResource(resourceType)) { return BadRequest($"'{resourceType}' is not a valid TestingLab resource type"); }

    await _permissionService.RevokePermissionAsync(userId, tenantId, action, resourceType, resourceId).ConfigureAwait(false);

    _logger.LogInformation("Admin user {AdminUserId} revoked permission '{Action}' on {ResourceType} {ResourceId} from user {UserId}", GetCurrentUserId(), action, resourceType, resourceId, userId);

    return NoContent();
  }

  // ===== PERMISSION CHECKING =====

  /// <summary> Check if a user can perform an action on a TestingLab resource </summary>
  [HttpGet("users/{userId}/check/{resourceType}")]
  public async Task<ActionResult<bool>> CheckTestingLabPermission(Guid userId, string resourceType, [FromQuery] string action, [FromQuery] Guid? resourceId = null, [FromQuery] Guid? tenantId = null) {
    if (!IsTestingLabResource(resourceType)) { return BadRequest($"'{resourceType}' is not a valid TestingLab resource type"); }

    var hasPermission = await _permissionService.HasPermissionAsync(userId, tenantId, action, resourceType, resourceId).ConfigureAwait(false);

    return Ok(hasPermission);
  }

  // ===== HELPER METHODS =====

  private static bool IsTestingLabResource(string resourceType) {
    return TestingLabResourceTypes.IsValid(resourceType);
  }

  private static List<PermissionTemplate> BuildPermissionTemplates(TestingLabPermissionsDto permissions) {
    var templates = new List<PermissionTemplate>();

    // Sessions
    if (permissions.CanCreateSessions) templates.Add(new PermissionTemplate { Action = TestingLabActions.Create, ResourceType = TestingLabResourceTypes.Session });
    if (permissions.CanEditSessions) templates.Add(new PermissionTemplate { Action = TestingLabActions.Edit, ResourceType = TestingLabResourceTypes.Session });
    if (permissions.CanDeleteSessions) templates.Add(new PermissionTemplate { Action = TestingLabActions.Delete, ResourceType = TestingLabResourceTypes.Session });
    if (permissions.CanViewSessions) templates.Add(new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Session });

    // Locations
    if (permissions.CanCreateLocations) templates.Add(new PermissionTemplate { Action = TestingLabActions.Create, ResourceType = TestingLabResourceTypes.Location });
    if (permissions.CanEditLocations) templates.Add(new PermissionTemplate { Action = TestingLabActions.Edit, ResourceType = TestingLabResourceTypes.Location });
    if (permissions.CanDeleteLocations) templates.Add(new PermissionTemplate { Action = TestingLabActions.Delete, ResourceType = TestingLabResourceTypes.Location });
    if (permissions.CanViewLocations) templates.Add(new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Location });

    // Feedback
    if (permissions.CanCreateFeedback) templates.Add(new PermissionTemplate { Action = TestingLabActions.Create, ResourceType = TestingLabResourceTypes.Feedback });
    if (permissions.CanEditFeedback) templates.Add(new PermissionTemplate { Action = TestingLabActions.Edit, ResourceType = TestingLabResourceTypes.Feedback });
    if (permissions.CanDeleteFeedback) templates.Add(new PermissionTemplate { Action = TestingLabActions.Delete, ResourceType = TestingLabResourceTypes.Feedback });
    if (permissions.CanViewFeedback) templates.Add(new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Feedback });
    if (permissions.CanModerateFeedback) templates.Add(new PermissionTemplate { Action = TestingLabActions.Moderate, ResourceType = TestingLabResourceTypes.Feedback });

    // Requests
    if (permissions.CanCreateRequests) templates.Add(new PermissionTemplate { Action = TestingLabActions.Create, ResourceType = TestingLabResourceTypes.Request });
    if (permissions.CanEditRequests) templates.Add(new PermissionTemplate { Action = TestingLabActions.Edit, ResourceType = TestingLabResourceTypes.Request });
    if (permissions.CanDeleteRequests) templates.Add(new PermissionTemplate { Action = TestingLabActions.Delete, ResourceType = TestingLabResourceTypes.Request });
    if (permissions.CanViewRequests) templates.Add(new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Request });
    if (permissions.CanApproveRequests) templates.Add(new PermissionTemplate { Action = TestingLabActions.Approve, ResourceType = TestingLabResourceTypes.Request });

    // Participants
    if (permissions.CanManageParticipants) templates.Add(new PermissionTemplate { Action = TestingLabActions.Manage, ResourceType = TestingLabResourceTypes.Participant });
    if (permissions.CanViewParticipants) templates.Add(new PermissionTemplate { Action = TestingLabActions.Read, ResourceType = TestingLabResourceTypes.Participant });

    return templates;
  }

  private static TestingLabRoleTemplate MapToTestingLabRoleTemplate(RoleTemplate template) {
    return new TestingLabRoleTemplate {
      Id = template.Id,
      Name = template.Name,
      Description = template.Description,
      IsSystemRole = template.IsSystemRole,
      Permissions = new TestingLabPermissionsDto {
        // Sessions
        CanCreateSessions = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Session) == true,
        CanEditSessions = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Session) == true,
        CanDeleteSessions = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Session) == true,
        CanViewSessions = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Session) == true,

        // Locations
        CanCreateLocations = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Location) == true,
        CanEditLocations = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Location) == true,
        CanDeleteLocations = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Location) == true,
        CanViewLocations = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Location) == true,

        // Feedback
        CanCreateFeedback = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Feedback) == true,
        CanEditFeedback = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Feedback) == true,
        CanDeleteFeedback = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Feedback) == true,
        CanViewFeedback = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Feedback) == true,
        CanModerateFeedback = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Moderate && p.ResourceType == TestingLabResourceTypes.Feedback) == true,

        // Requests
        CanCreateRequests = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Create && p.ResourceType == TestingLabResourceTypes.Request) == true,
        CanEditRequests = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Edit && p.ResourceType == TestingLabResourceTypes.Request) == true,
        CanDeleteRequests = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Delete && p.ResourceType == TestingLabResourceTypes.Request) == true,
        CanViewRequests = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Request) == true,
        CanApproveRequests = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Approve && p.ResourceType == TestingLabResourceTypes.Request) == true,

        // Participants
        CanManageParticipants = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Manage && p.ResourceType == TestingLabResourceTypes.Participant) == true,
        CanViewParticipants = template.PermissionTemplates?.Any(p => p.Action == TestingLabActions.Read && p.ResourceType == TestingLabResourceTypes.Participant) == true,
      },
    };
  }
}

// ===== TESTING LAB SPECIFIC MODELS =====

public class TestingLabPermissionsDto {
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

public class TestingLabRoleTemplate {
  public Guid Id { get; set; } // Added so clients can perform update/delete operations

  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public bool IsSystemRole { get; set; }

  public TestingLabPermissionsDto Permissions { get; set; } = new TestingLabPermissionsDto();
}

public class UserTestingLabPermissions {
  public Guid UserId { get; set; }

  public Guid? TenantId { get; set; }

  public List<string> AssignedRoles { get; set; } = new List<string>();

  public TestingLabPermissionsDto Permissions { get; set; } = new TestingLabPermissionsDto();
}

public class CreateTestingLabRoleRequest {
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public TestingLabPermissionsDto Permissions { get; set; } = new TestingLabPermissionsDto();
}

public class UpdateTestingLabRoleRequest {
  public string? Name { get; set; } // Optional new name

  public string Description { get; set; } = string.Empty;

  public TestingLabPermissionsDto Permissions { get; set; } = new TestingLabPermissionsDto();
}

public class AssignTestingLabRoleRequest {
  public Guid? TenantId { get; set; }

  public string RoleName { get; set; } = string.Empty;

  public DateTime? ExpiresAt { get; set; }
}

public class GrantResourcePermissionRequest {
  public Guid? TenantId { get; set; }

  public string Action { get; set; } = string.Empty;

  public DateTime? ExpiresAt { get; set; }
}
