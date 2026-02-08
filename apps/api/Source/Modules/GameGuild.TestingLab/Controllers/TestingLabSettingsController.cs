using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace GameGuild.TestingLab;

/// <summary> Controller for TestingLabSettings operations </summary>
[Route("api/testing-lab/settings")]
[Authorize]
public class TestingLabSettingsController(
  ITestingLabSettingsService settingsService,
  ITenantService tenantService,
  IActorContextAccessor actorContextAccessor,
  ILogger<TestingLabSettingsController> _logger
) : BaseApiController {
  private ActorContext Actor => actorContextAccessor.ActorContext;
  /// <summary> Get testing lab settings for the current tenant or global settings if no tenant context Creates default settings if none exist </summary>
  [HttpGet]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Read)]
  public async Task<ActionResult<TestingLabSettingsDto>> GetSettings() {
    var userId = Actor.SubjectIdAsGuid;
    if (userId == null) { return Unauthorized(new { message = "User ID claim not found in token" }); }

    // Use middleware-provided tenant context (nullable for global)
    var tenantId = Actor.TenantId;

    var settings = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

    return Ok(settings);
  }

  /// <summary> Create or update testing lab settings for the current tenant </summary>
  [HttpPut]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> CreateOrUpdateSettings([FromBody] CreateTestingLabSettingsDto dto) {
    try {
      // Allow null tenant (global) – pass through nullable context
      var tenantId = Actor.TenantId; // may be null for global settings
      await settingsService.CreateOrUpdateTestingLabSettingsAsync(tenantId, dto);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
  }

  /// <summary> Update testing lab settings for the current tenant (partial update) </summary>
  [HttpPatch]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> UpdateSettings([FromBody] UpdateTestingLabSettingsDto dto) {
    try {
      var tenantId = Actor.TenantId; // nullable allowed
      await settingsService.UpdateTestingLabSettingsAsync(tenantId, dto);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
  }

  /// <summary> Reset testing lab settings to default values for the current tenant </summary>
  [HttpPost("reset")]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Edit)]
  public async Task<ActionResult<TestingLabSettingsDto>> ResetSettings() {
    try {
      var tenantId = Actor.TenantId;
      await settingsService.ResetTestingLabSettingsAsync(tenantId);
      var settingsDto = await settingsService.GetTestingLabSettingsDtoAsync(tenantId);

      return Ok(settingsDto);
    }
    catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
  }

  /// <summary> Check if testing lab settings exist for the current tenant </summary>
  [HttpGet("exists")]
  [RequireContentTypePermission<TestingLabSettings>(PermissionType.Read)]
  public async Task<ActionResult<bool>> SettingsExist() {
    var tenantId = Actor.TenantId;
    var exists = await settingsService.TestingLabSettingsExistAsync(tenantId);

    return Ok(exists);
  }

  #region Private Helper Methods

  /// <summary> Get the current tenant ID from the request context </summary>
  private async Task<Guid?> GetCurrentTenantIdAsync() {
    // First, try to get tenant ID from claims
    // Check for both standard claim "tenant_id" and JWT-specific claim
    var tenantIdClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

    if (Guid.TryParse(tenantIdClaim, out var tenantId)) { return tenantId; }

    // If not in claims, try to get from tenant service using current user
    var userGuid = Actor.SubjectIdAsGuid;

    if (userGuid.HasValue) {
      var tenantPermissions = await tenantService.GetTenantsForUserAsync(userGuid.Value).ConfigureAwait(false);
      var firstTenant = tenantPermissions.FirstOrDefault()?.Tenant;

      return firstTenant?.Id;
    }

    return null;
  }

  #endregion
}
