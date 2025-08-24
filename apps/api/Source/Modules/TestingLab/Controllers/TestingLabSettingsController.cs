using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.TestingLab.Dtos;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.TestingLab.Controllers;

/// <summary> Controller for TestingLabSettings operations </summary>
[ApiController]
[Route("api/testing-lab/settings")]
public class TestingLabSettingsController(
  ITestingLabSettingsService settingsService,
  ITenantService tenantService,
  ITenantContext tenantContext // prefer middleware-provided tenant context
) : ControllerBase {
  /// <summary> Get testing lab settings for the current tenant or global settings if no tenant context Creates default settings if none exist </summary>
  [HttpGet]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Read)]
  public async Task<ActionResult<TestingLabSettingsDto>> GetSettings() {
    try {
      // Enhanced debugging: Log all claims
      var claims = User.Claims.Select(c => new {
                           c.Type, c.Value,
                         }
                       )
                       .ToList();
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var rolesClaim = User.FindFirst("roles")?.Value;
      var roleClaims = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

      // For debugging purposes only - log role information
      Console.WriteLine($"User ID: {userId}");
      Console.WriteLine($"Roles claim: {rolesClaim}");
      Console.WriteLine($"Individual role claims: {string.Join(", ", roleClaims)}");

      if (string.IsNullOrEmpty(userId)) {
        return Unauthorized(
          new {
            message = "User ID claim not found in token", allClaims = claims,
          }
        );
      }

      // Use middleware-provided tenant context (nullable for global)
      var tenantId = tenantContext.TenantId;

      var settings = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settings);
    }
    catch (Exception ex) {
      // Enhanced error response with debugging information
      return StatusCode(
        500,
        new {
          message = "An error occurred while retrieving settings",
          error = ex.Message,
          stackTrace = ex.StackTrace,
          claims = User.Claims.Select(c => new {
                           c.Type, c.Value,
                         }
                       )
                       .ToList(),
          tenantInfo = new {
            hasTenantClaim = User.HasClaim(c => c.Type == "tenant_id" || c.Type == "http://schemas.microsoft.com/identity/claims/tenantid"),
            tenantIdClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value,
          },
        }
      );
    }
  }

  /// <summary> Create or update testing lab settings for the current tenant </summary>
  [HttpPut]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> CreateOrUpdateSettings([FromBody] CreateTestingLabSettingsDto dto) {
    try {
      // Allow null tenant (global) – pass through nullable context
      var tenantId = tenantContext.TenantId; // may be null for global settings
      await settingsService.CreateOrUpdateTestingLabSettingsAsync(tenantId, dto);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) {
      return BadRequest(
        new {
          message = ex.Message,
        }
      );
    }
    catch (Exception ex) {
      return StatusCode(
        500,
        new {
          message = "An error occurred while saving settings", error = ex.Message,
        }
      );
    }
  }

  /// <summary> Update testing lab settings for the current tenant (partial update) </summary>
  [HttpPatch]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> UpdateSettings([FromBody] UpdateTestingLabSettingsDto dto) {
    try {
      var tenantId = tenantContext.TenantId; // nullable allowed
      await settingsService.UpdateTestingLabSettingsAsync(tenantId, dto);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) {
      return BadRequest(
        new {
          message = ex.Message,
        }
      );
    }
    catch (Exception ex) {
      return StatusCode(
        500,
        new {
          message = "An error occurred while updating settings", error = ex.Message,
        }
      );
    }
  }

  /// <summary> Reset testing lab settings to default values for the current tenant </summary>
  [HttpPost("reset")]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> ResetSettings() {
    try {
      var tenantId = tenantContext.TenantId;
      await settingsService.ResetTestingLabSettingsAsync(tenantId);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) {
      return BadRequest(
        new {
          message = ex.Message,
        }
      );
    }
    catch (Exception ex) {
      return StatusCode(
        500,
        new {
          message = "An error occurred while resetting settings", error = ex.Message,
        }
      );
    }
  }

  /// <summary> Check if testing lab settings exist for the current tenant </summary>
  [HttpGet("exists")]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Read)]
  public async Task<ActionResult<bool>> SettingsExist() {
    try {
      var tenantId = tenantContext.TenantId;
      var exists = await settingsService.TestingLabSettingsExistAsync(tenantId);

      return Ok(exists);
    }
    catch (Exception ex) {
      return StatusCode(
        500,
        new {
          message = "An error occurred while checking settings", error = ex.Message,
        }
      );
    }
  }

  #region Private Helper Methods

  /// <summary> Get the current tenant ID from the request context </summary>
  private async Task<Guid?> GetCurrentTenantIdAsync() {
    // First, try to get tenant ID from claims
    // Check for both standard claim "tenant_id" and JWT-specific claim
    var tenantIdClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

    if (Guid.TryParse(tenantIdClaim, out var tenantId)) { return tenantId; }

    // If not in claims, try to get from tenant service using current user
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (Guid.TryParse(userId, out var userGuid)) {
      var tenantPermissions = await tenantService.GetTenantsForUserAsync(userGuid).ConfigureAwait(false);
      var firstTenant = tenantPermissions.FirstOrDefault()?.Tenant;

      return firstTenant?.Id;
    }

    return null;
  }

  #endregion
}
