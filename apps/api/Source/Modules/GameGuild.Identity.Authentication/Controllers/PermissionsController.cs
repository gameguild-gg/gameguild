using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// TODO: Reactivate this controller when permission management features are ready for production
/// <summary>
///     API controller for comprehensive permission management operations
///     Enhanced with CQRS pattern, 3-layer permission hierarchy, and advanced analytics
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Tags("permissions")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PermissionsController(IMediator mediator, ILogger<PermissionsController> logger) : ControllerBase
{
    private readonly ILogger<PermissionsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Tenant GameGuild.Permissions

    /// <summary>
    ///     Grant tenant permissions to a user
    /// </summary>
    [HttpPost("tenant/grant")]
    public async Task<ActionResult<TenantPermission>> GrantTenantPermission([FromBody] GrantTenantPermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant tenant permissions to user {UserId} for tenant {TenantId}", command.UserId, command.TenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke tenant permissions from a user
    /// </summary>
    [HttpPost("tenant/revoke")]
    public async Task<ActionResult> RevokeTenantPermission([FromBody] RevokeTenantPermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tenant permissions from user {UserId} for tenant {TenantId}", command.UserId, command.TenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Check if user has a specific tenant permission
    /// </summary>
    [HttpPost("tenant/check")]
    public async Task<ActionResult<bool>> HasTenantPermission([FromBody] HasTenantPermissionQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check tenant permission {Permission} for user {UserId}", query.Permission, query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get all tenant permissions for a user
    /// </summary>
    [HttpPost("tenant/list")]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetTenantPermissions([FromBody] GetTenantPermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant permissions for user {UserId} in tenant {TenantId}", query.UserId, query.TenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk grant tenant permissions
    /// </summary>
    [HttpPost("tenant/bulk-grant")]
    public async Task<ActionResult<BulkPermissionResult>> BulkGrantTenantPermissions([FromBody] BulkGrantTenantPermissionsCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk grant tenant permissions for {UserCount} users", command.UserIds.Count);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk revoke tenant permissions
    /// </summary>
    [HttpPost("tenant/bulk-revoke")]
    public async Task<ActionResult<BulkPermissionResult>> BulkRevokeTenantPermissions([FromBody] BulkRevokeTenantPermissionsCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk revoke tenant permissions for {UserCount} users", command.UserIds.Count);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Content Type GameGuild.Permissions

    /// <summary>
    ///     Grant content type permissions to a user
    /// </summary>
    [HttpPost("content-type/grant")]
    public async Task<ActionResult<ContentTypePermission>> GrantContentTypePermission([FromBody] GrantContentTypePermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant content type permissions to user {UserId} for content type {ContentType}", command.UserId, command.ContentType);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke content type permissions from a user
    /// </summary>
    [HttpPost("content-type/revoke")]
    public async Task<ActionResult> RevokeContentTypePermission([FromBody] RevokeContentTypePermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke content type permissions from user {UserId} for content type {ContentType}", command.UserId, command.ContentType);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Check if user has a specific content type permission
    /// </summary>
    [HttpPost("content-type/check")]
    public async Task<ActionResult<bool>> HasContentTypePermission([FromBody] HasContentTypePermissionQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check content type permission {Permission} for user {UserId}", query.Permission, query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get all content type permissions for a user
    /// </summary>
    [HttpPost("content-type/list")]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetContentTypePermissions([FromBody] GetContentTypePermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content type permissions for user {UserId}", query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Resource GameGuild.Permissions

    /// <summary>
    ///     Grant resource-level permissions to a user
    /// </summary>
    [HttpPost("resource/grant")]
    public async Task<ActionResult<bool>> GrantResourcePermission([FromBody] GrantResourcePermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant resource permissions to user {UserId} for resource {ResourceId}", command.UserId, command.ResourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke resource-level permissions from a user
    /// </summary>
    [HttpPost("resource/revoke")]
    public async Task<ActionResult> RevokeResourcePermission([FromBody] RevokeResourcePermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke resource permissions from user {UserId} for resource {ResourceId}", command.UserId, command.ResourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Check if user has a specific resource permission
    /// </summary>
    [HttpPost("resource/check")]
    public async Task<ActionResult<bool>> HasResourcePermission([FromBody] HasResourcePermissionQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resource permission {Permission} for user {UserId}", query.Permission, query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get all resource permissions for a user
    /// </summary>
    [HttpPost("resource/list")]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetResourcePermissions([FromBody] GetResourcePermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource permissions for user {UserId} and resource {ResourceId}", query.UserId, query.ResourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Bulk grant resource permissions
    /// </summary>
    [HttpPost("resource/bulk-grant")]
    public async Task<ActionResult<BulkPermissionResult>> BulkGrantResourcePermissions([FromBody] BulkGrantResourcePermissionsCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk grant resource permissions for {UserCount} users", command.UserIds.Count);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Unified Permission Operations

    /// <summary>
    ///     Get all permissions for a user across all layers (tenant + content-type + resource)
    /// </summary>
    [HttpPost("user/all")]
    public async Task<ActionResult<UserPermissionsDto>> GetUserPermissions([FromBody] GetUserPermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all user permissions for user {UserId}", query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get effective permissions for a user (resolved through all layers with inheritance)
    /// </summary>
    [HttpPost("user/effective")]
    public async Task<ActionResult<EffectivePermissionsDto>> GetEffectivePermissions([FromBody] GetEffectivePermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effective permissions for user {UserId}", query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Resolve permission hierarchy for a specific permission check
    /// </summary>
    [HttpPost("hierarchy/resolve")]
    public async Task<ActionResult<PermissionHierarchyResult>> ResolvePermissionHierarchy([FromBody] ResolvePermissionHierarchyQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve permission hierarchy for user {UserId}", query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Permission Analytics

    /// <summary>
    ///     Get permission usage analytics for a tenant
    /// </summary>
    [HttpGet("analytics/{tenantId}")]
    public async Task<ActionResult<PermissionAnalyticsDto>> GetPermissionAnalytics(Guid tenantId, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetPermissionAnalyticsQuery { TenantId = tenantId, FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30), ToDate = toDate ?? DateTime.UtcNow };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission analytics for tenant {TenantId}", tenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get permission audit trail for compliance reporting
    /// </summary>
    [HttpPost("audit/trail")]
    public async Task<ActionResult<PermissionAuditTrailDto>> GetPermissionAuditTrail([FromBody] GetPermissionAuditTrailQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission audit trail for user {UserId}", query.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get cache statistics for permission system performance monitoring
    /// </summary>
    [HttpGet("cache/stats")]
    public async Task<ActionResult<PermissionCacheStatsDto>> GetCacheStatistics()
    {
        try
        {
            var query = new GetPermissionCacheStatsQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission cache statistics");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Clear permission cache for a specific user or tenant
    /// </summary>
    [HttpDelete("cache/clear")]
    public async Task<ActionResult> ClearPermissionCache([FromQuery] Guid? userId = null, [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var command = new ClearPermissionCacheCommand { UserId = userId, TenantId = tenantId };
            await _mediator.Send(command);

            return Ok(new { message = "Permission cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear permission cache");

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Permission Templates

    /// <summary>
    ///     Get available permission templates for common roles
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<PermissionTemplateDto>>> GetPermissionTemplates()
    {
        try
        {
            var query = new GetPermissionTemplatesQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission templates");

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Apply a permission template to a user
    /// </summary>
    [HttpPost("templates/apply")]
    public async Task<ActionResult<ApplyPermissionTemplateResult>> ApplyPermissionTemplate([FromBody] ApplyPermissionTemplateCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply permission template {TemplateId} to user {UserId}", command.TemplateId, command.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion
}
