using GameGuild.Common;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Tenants;

[ApiController]
[Route("api/tenant-domains")]
public class TenantDomainController(ITenantDomainService tenantDomainService) : ControllerBase {
  #region Domain Management

  // GET: api/tenant-domains
  [HttpGet]
  [RequireResourcePermission<TenantDomain>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TenantDomain>>> GetDomains([FromQuery] Guid? tenantId) {
    if (tenantId.HasValue) {
      var domains = await tenantDomainService.GetDomainsByTenantAsync(tenantId.Value);

      return Ok(domains);
    }

    // If no tenant specified, return all domains (requires admin permissions via the attribute)
    var allDomains = await tenantDomainService.GetAllDomainsAsync();

    return Ok(allDomains);
  }

  // GET: api/tenant-domains/{id}
  [HttpGet("{id}")]
  [RequireResourcePermission<TenantDomain>(PermissionType.Read)]
  public async Task<ActionResult<TenantDomain>> GetDomain(Guid id) {
    var domain = await tenantDomainService.GetDomainByIdAsync(id);

    if (domain == null) return NotFound();

    return Ok(domain);
  }

  // POST: api/tenant-domains
  [HttpPost]
  [RequireResourcePermission<TenantDomain>(PermissionType.Create)]
  public async Task<ActionResult<TenantDomain>> CreateDomain([FromBody] CreateTenantDomainDto request) {
    try {
      // Validate domain ownership (in a real implementation)
      var isValid = await tenantDomainService.ValidateDomainOwnershipAsync(request.TenantId, request.TopLevelDomain);

      if (!isValid) return BadRequest("Domain ownership could not be validated");

      var domain = request.ToTenantDomain();
      var createdDomain = await tenantDomainService.CreateDomainAsync(domain);

      return CreatedAtAction(nameof(GetDomain), new { id = createdDomain.Id }, createdDomain);
    }
    catch (Exception ex) { return BadRequest($"Error creating domain: {ex.Message}"); }
  }

  // PUT: api/tenant-domains/{id}
  [HttpPut("{id}")]
  [RequireResourcePermission<TenantDomain>(PermissionType.Edit)]
  public async Task<ActionResult<TenantDomain>> UpdateDomain(Guid id, [FromBody] UpdateTenantDomainDto request) {
    try {
      var existingDomain = await tenantDomainService.GetDomainByIdAsync(id);

      if (existingDomain == null) return NotFound();

      request.UpdateTenantDomain(existingDomain);
      var updatedDomain = await tenantDomainService.UpdateDomainAsync(existingDomain);

      return Ok(updatedDomain);
    }
    catch (Exception ex) { return BadRequest($"Error updating domain: {ex.Message}"); }
  }

  // DELETE: api/tenant-domains/{id}
  [HttpDelete("{id}")]
  [RequireResourcePermission<TenantDomain>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteDomain(Guid id) {
    var result = await tenantDomainService.DeleteDomainAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  // POST: api/tenant-domains/{tenantId}/set-main/{domainId}
  [HttpPost("{tenantId}/set-main/{domainId}")]
  [RequireResourcePermission<TenantDomain>(PermissionType.Edit)]
  public async Task<ActionResult> SetMainDomain(Guid tenantId, Guid domainId) {
    var result = await tenantDomainService.SetMainDomainAsync(tenantId, domainId);

    if (!result) return NotFound();

    return Ok();
  }

  #endregion

  #region User Group Management // GET: api/tenant-domains/user-groups

  [HttpGet("user-groups")]
  [RequireResourcePermission<TenantUserGroup>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TenantUserGroupDto>>> GetUserGroups(
    [FromQuery] Guid tenantId,
    [FromQuery] bool rootOnly = false
  ) {
    var userGroups = rootOnly
                       ? await tenantDomainService.GetRootUserGroupsByTenantAsync(tenantId)
                       : await tenantDomainService.GetUserGroupsByTenantAsync(tenantId);

    var dtos = userGroups.Select(TenantUserGroupDto.FromTenantUserGroup);

    return Ok(dtos);
  } // GET: api/tenant-domains/user-groups/{id}

  [HttpGet("user-groups/{id}")]
  [RequireResourcePermission<TenantUserGroup>(PermissionType.Read)]
  public async Task<ActionResult<TenantUserGroupDto>> GetUserGroup(Guid id) {
    var userGroup = await tenantDomainService.GetUserGroupByIdAsync(id);

    if (userGroup == null) return NotFound();

    var dto = TenantUserGroupDto.FromTenantUserGroup(userGroup);

    return Ok(dto);
  } // POST: api/tenant-domains/user-groups

  [HttpPost("user-groups")]
  [RequireResourcePermission<TenantUserGroup>(PermissionType.Create)]
  public async Task<ActionResult<TenantUserGroupDto>> CreateUserGroup([FromBody] CreateTenantUserGroupDto request) {
    try {
      var userGroup = request.ToTenantUserGroup();
      var createdUserGroup = await tenantDomainService.CreateUserGroupAsync(userGroup);
      var dto = TenantUserGroupDto.FromTenantUserGroup(createdUserGroup);

      return CreatedAtAction(nameof(GetUserGroup), new { id = createdUserGroup.Id }, dto);
    }
    catch (Exception ex) { return BadRequest($"Error creating user group: {ex.Message}"); }
  } // PUT: api/tenant-domains/user-groups/{id}

  [HttpPut("user-groups/{id}")]
  [RequireResourcePermission<TenantUserGroup>(PermissionType.Edit)]
  public async Task<ActionResult<TenantUserGroupDto>> UpdateUserGroup(
    Guid id,
    [FromBody] UpdateTenantUserGroupDto request
  ) {
    try {
      var existingUserGroup = await tenantDomainService.GetUserGroupByIdAsync(id);

      if (existingUserGroup == null) return NotFound();

      request.UpdateTenantUserGroup(existingUserGroup);
      var updatedUserGroup = await tenantDomainService.UpdateUserGroupAsync(existingUserGroup);
      var dto = TenantUserGroupDto.FromTenantUserGroup(updatedUserGroup);

      return Ok(dto);
    }
    catch (Exception ex) { return BadRequest($"Error updating user group: {ex.Message}"); }
  }

  // DELETE: api/tenant-domains/user-groups/{id}
  [HttpDelete("user-groups/{id}")]
  [RequireResourcePermission<TenantUserGroup>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteUserGroup(Guid id) {
    var result = await tenantDomainService.DeleteUserGroupAsync(id);

    if (!result) return NotFound();

    return NoContent();
  }

  #endregion

  #region User Group Membership Management

  // GET: api/tenant-domains/memberships/user/{userId}
  [HttpGet("memberships/user/{userId}")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TenantUserGroupMembershipDto>>> GetUserMemberships(Guid userId) {
    var memberships = await tenantDomainService.GetUserGroupMembershipsAsync(userId);
    var dtos = memberships.Select(m => new TenantUserGroupMembershipDto {
                                    Id = m.Id,
                                    UserId = m.UserId,
                                    GroupId = m.UserGroupId,
                                    IsAutoAssigned = m.IsAutoAssigned,
                                    CreatedAt = m.CreatedAt,
      }
    );

    return Ok(dtos);
  }

  // POST: api/tenant-domains/user-groups/memberships
  [HttpPost("user-groups/memberships")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Create)]
  public async Task<ActionResult<TenantUserGroupMembership>> AddUserToGroup([FromBody] AddUserToGroupDto request) {
    try {
      var membership =
        await tenantDomainService.AddUserToGroupAsync(request.UserId, request.UserGroupId, request.IsAutoAssigned);

      return Ok(membership);
    }
    catch (Exception ex) { return BadRequest($"Error adding user to group: {ex.Message}"); }
  }

  // DELETE: api/tenant-domains/user-groups/memberships
  [HttpDelete("user-groups/memberships")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Delete)]
  public async Task<ActionResult> RemoveUserFromGroup([FromQuery] Guid userId, [FromQuery] Guid userGroupId) {
    var result = await tenantDomainService.RemoveUserFromGroupAsync(userId, userGroupId);

    if (!result) return NotFound();

    return NoContent();
  }

  // GET: api/tenant-domains/user-groups/{groupId}/members
  [HttpGet("user-groups/{groupId}/members")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TenantUserGroupMembership>>> GetGroupMembers(Guid groupId) {
    var members = await tenantDomainService.GetGroupMembersAsync(groupId);

    return Ok(members);
  } // GET: api/tenant-domains/users/{userId}/groups

  [HttpGet("users/{userId}/groups")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<TenantUserGroupDto>>> GetUserGroups(Guid userId) {
    var userGroups = await tenantDomainService.GetUserGroupsForUserAsync(userId);
    var dtos = userGroups.Select(TenantUserGroupDto.FromTenantUserGroup);

    return Ok(dtos);
  }

  // GET: api/tenant-domains/groups/{groupId}/users  
  [HttpGet("groups/{groupId}/users")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<UserDto>>> GetGroupUsers(Guid groupId) {
    var users = await tenantDomainService.GetUsersInGroupAsync(groupId);
    var userDtos = users.Select(u => new UserDto { Id = u.Id, Username = u.Name, Email = u.Email });

    return Ok(userDtos);
  }

  #endregion

  #region Auto-Assignment

  // POST: api/tenant-domains/auto-assign
  [HttpPost("auto-assign")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Create)]
  public async Task<ActionResult<TenantUserGroupMembershipDto>> AutoAssignUser([FromBody] AutoAssignUserDto request) {
    try {
      var membership = await tenantDomainService.AutoAssignUserToGroupsAsync(request.Email);

      var tenantUserGroupMemberships = membership as TenantUserGroupMembership[] ?? membership.ToArray();

      if (tenantUserGroupMemberships.Length == 0) return NotFound("No matching domain found for user's email");

      // Return the first membership created (there should typically be only one for a single user)
      var firstMembership = tenantUserGroupMemberships.First();

      var membershipDto = new TenantUserGroupMembershipDto {
        Id = firstMembership.Id,
        UserId = firstMembership.UserId,
        GroupId = firstMembership.UserGroupId,
        IsAutoAssigned = firstMembership.IsAutoAssigned,
        CreatedAt = firstMembership.CreatedAt,
      };

      return CreatedAtAction(nameof(GetUserGroups), new { userId = firstMembership.UserId }, membershipDto);

    }
    catch (Exception ex) { return BadRequest($"Error during auto-assignment: {ex.Message}"); }
  }

  // POST: api/tenant-domains/auto-assign-bulk
  [HttpPost("auto-assign-bulk")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Create)]
  public async Task<ActionResult<object>> AutoAssignUsers([FromBody] AutoAssignUsersDto request) {
    try {
      int assignedCount;

      if (request.UserEmails != null && request.UserEmails.Any()) {
        // Auto-assign specific users
        var memberships = new List<TenantUserGroupMembership>();

        foreach (var email in request.UserEmails) {
          var userMemberships = await tenantDomainService.AutoAssignUserToGroupsAsync(email);
          memberships.AddRange(userMemberships);
        }

        assignedCount = memberships.Count;
      }
      else if (request.TenantId.HasValue) {
        // Auto-assign all users for a specific tenant
        assignedCount = await tenantDomainService.AutoAssignTenantUsersAsync(request.TenantId.Value);
      }
      else {
        // Auto-assign all users (admin operation)
        assignedCount = await tenantDomainService.AutoAssignAllUsersAsync();
      }

      return Ok(new { AssignedCount = assignedCount });
    }
    catch (Exception ex) { return BadRequest($"Error during auto-assignment: {ex.Message}"); }
  }

  // GET: api/tenant-domains/domain-match
  [HttpGet("domain-match")]
  [RequireResourcePermission<TenantDomain>(PermissionType.Read)]
  public async Task<ActionResult<TenantDomain>> FindMatchingDomain([FromQuery] string email) {
    var domain = await tenantDomainService.FindMatchingDomainAsync(email);

    if (domain == null) return NotFound("No matching domain found for the provided email");

    return Ok(domain);
  }

  #endregion

  #region Test Membership Endpoint

  // POST: api/tenant-domains/memberships
  [HttpPost("memberships")]
  [RequireResourcePermission<TenantUserGroupMembership>(PermissionType.Create)]
  public async Task<ActionResult<TenantUserGroupMembershipDto>> AddUserToGroupMembership([FromBody] AddUserToGroupDto request) {
    try {
      var membership =
        await tenantDomainService.AddUserToGroupAsync(request.UserId, request.UserGroupId, request.IsAutoAssigned);
      var membershipDto = new TenantUserGroupMembershipDto {
        Id = membership.Id,
        UserId = membership.UserId,
        GroupId = membership.UserGroupId,
        IsAutoAssigned = membership.IsAutoAssigned,
        CreatedAt = membership.CreatedAt,
      };

      return CreatedAtAction(nameof(GetUserGroups), new { userId = membership.UserId }, membershipDto);
    }
    catch (Exception ex) { return BadRequest($"Error adding user to group: {ex.Message}"); }
  }

  #endregion
}
