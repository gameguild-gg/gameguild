using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Identity.Authorization;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

// TODO: Reactivate this controller when permission management features are ready for production
/// <summary>
///     API controller for comprehensive permission management operations
///     Enhanced with CQRS pattern, 3-layer permission hierarchy, and advanced analytics
///     
///     Resource hierarchy:
///     - /v1/permissions/tenant-grants - Tenant-level permission grants
///     - /v1/permissions/content-type-grants - Content type permission grants  
///     - /v1/permissions/resource-grants - Resource-level permission grants
///     - /v1/permissions/templates - Permission templates
///     - /v1/permissions/cache - Cache management
///     - /v1/permissions/audit-trail - Audit trail
/// </summary>
/// <remarks>
///     Rate limited to 100 requests per minute per client to prevent DoS attacks on permission evaluation.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/permissions")]
[Tags("permissions")]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableRateLimiting(RateLimitPolicies.Authorization)]
public class PermissionsController(IMediator mediator, ILogger<PermissionsController> logger) : ControllerBase
{
    private readonly ILogger<PermissionsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    #region Tenant Permission Grants

    /// <summary>
    ///     Create a tenant permission grant for a user
    /// </summary>
    [HttpPost("tenant-grants")]
    [ProducesResponseType(typeof(TenantPermission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TenantPermission>> CreateTenantGrant([FromBody] GrantTenantPermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetTenantPermissions), new { userId = command.UserId, tenantId = command.TenantId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant tenant permissions to user {UserId} for tenant {TenantId}", command.UserId, command.TenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete a tenant permission grant
    /// </summary>
    [HttpDelete("tenant-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTenantGrant(Guid grantId)
    {
        try
        {
            var command = new RevokeTenantPermissionByIdCommand { GrantId = grantId };
            await _mediator.Send(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tenant permission grant {GrantId}", grantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke tenant permissions from a user (legacy - use DELETE /tenant-grants/{grantId} instead)
    /// </summary>
    [HttpPost("tenant-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeTenantPermission([FromBody] RevokeTenantPermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return NoContent();
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
    [HttpPost("tenant:check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckTenantPermission([FromBody] HasTenantPermissionQuery query)
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
    [HttpGet("tenant")]
    [ProducesResponseType(typeof(IEnumerable<PermissionType>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetTenantPermissions(
        [FromQuery] Guid userId,
        [FromQuery] Guid tenantId)
    {
        try
        {
            var query = new GetTenantPermissionsQuery { UserId = userId, TenantId = tenantId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant permissions for user {UserId} in tenant {TenantId}", userId, tenantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Batch create tenant permission grants
    /// </summary>
    [HttpPost("tenant-grants:batch-create")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchCreateTenantGrants([FromBody] BulkGrantTenantPermissionsCommand command)
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
    ///     Batch delete tenant permission grants
    /// </summary>
    [HttpPost("tenant-grants:batch-delete")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchDeleteTenantGrants([FromBody] BulkRevokeTenantPermissionsCommand command)
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

    #region Content Type Permission Grants

    /// <summary>
    ///     Create a content type permission grant for a user
    /// </summary>
    [HttpPost("content-type-grants")]
    [ProducesResponseType(typeof(ContentTypePermission), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContentTypePermission>> CreateContentTypeGrant([FromBody] GrantContentTypePermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetContentTypePermissions), new { userId = command.UserId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant content type permissions to user {UserId} for content type {ContentType}", command.UserId, command.ContentType);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete a content type permission grant
    /// </summary>
    [HttpDelete("content-type-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteContentTypeGrant(Guid grantId)
    {
        try
        {
            var command = new RevokeContentTypePermissionByIdCommand { GrantId = grantId };
            await _mediator.Send(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke content type permission grant {GrantId}", grantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke content type permissions (legacy - use DELETE /content-type-grants/{grantId} instead)
    /// </summary>
    [HttpPost("content-type-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeContentTypePermission([FromBody] RevokeContentTypePermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return NoContent();
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
    [HttpPost("content-type:check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckContentTypePermission([FromBody] HasContentTypePermissionQuery query)
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
    [HttpGet("content-type")]
    [ProducesResponseType(typeof(IEnumerable<PermissionType>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetContentTypePermissions(
        [FromQuery] Guid userId,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? contentType = null)
    {
        try
        {
            var query = new GetContentTypePermissionsQuery { UserId = userId, TenantId = tenantId, ContentType = contentType };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content type permissions for user {UserId}", userId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Resource Permission Grants

    /// <summary>
    ///     Create a resource permission grant for a user
    /// </summary>
    [HttpPost("resource-grants")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CreateResourceGrant([FromBody] GrantResourcePermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetResourcePermissions), new { userId = command.UserId, resourceId = command.ResourceId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant resource permissions to user {UserId} for resource {ResourceId}", command.UserId, command.ResourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Delete a resource permission grant
    /// </summary>
    [HttpDelete("resource-grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteResourceGrant(Guid grantId)
    {
        try
        {
            var command = new RevokeResourcePermissionByIdCommand { GrantId = grantId };
            await _mediator.Send(command);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke resource permission grant {GrantId}", grantId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Revoke resource permissions (legacy - use DELETE /resource-grants/{grantId} instead)
    /// </summary>
    [HttpPost("resource-grants:revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeResourcePermission([FromBody] RevokeResourcePermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return NoContent();
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
    [HttpPost("resource:check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> CheckResourcePermission([FromBody] HasResourcePermissionQuery query)
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
    [HttpGet("resource")]
    [ProducesResponseType(typeof(IEnumerable<PermissionType>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetResourcePermissions(
        [FromQuery] Guid userId,
        [FromQuery] Guid resourceId)
    {
        try
        {
            var query = new GetResourcePermissionsQuery { UserId = userId, ResourceId = resourceId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource permissions for user {UserId} and resource {ResourceId}", userId, resourceId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Batch create resource permission grants
    /// </summary>
    [HttpPost("resource-grants:batch-create")]
    [ProducesResponseType(typeof(BulkPermissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionResult>> BatchCreateResourceGrants([FromBody] BulkGrantResourcePermissionsCommand command)
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

    #region User Permissions (Aggregated Views)

    /// <summary>
    ///     Get all permissions for a user across all layers (tenant + content-type + resource)
    /// </summary>
    [HttpGet("~/v{version:apiVersion}/users/{userId:guid}/permissions")]
    [ProducesResponseType(typeof(UserPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserPermissionsDto>> GetUserPermissions(Guid userId, [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var query = new GetUserPermissionsQuery { UserId = userId, TenantId = tenantId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all user permissions for user {UserId}", userId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get effective permissions for a user (resolved through all layers with inheritance)
    /// </summary>
    [HttpGet("~/v{version:apiVersion}/users/{userId:guid}/permissions/effective")]
    [ProducesResponseType(typeof(EffectivePermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EffectivePermissionsDto>> GetEffectivePermissions(
        Guid userId,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? resourceId = null)
    {
        try
        {
            var query = new GetEffectivePermissionsQuery { UserId = userId, TenantId = tenantId, ResourceId = resourceId };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effective permissions for user {UserId}", userId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Resolve permission hierarchy for a specific permission check
    /// </summary>
    [HttpPost(":resolve-hierarchy")]
    [ProducesResponseType(typeof(PermissionHierarchyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [HttpGet("~/v{version:apiVersion}/tenants/{tenantId:guid}/permissions/analytics")]
    [ProducesResponseType(typeof(PermissionAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionAnalyticsDto>> GetPermissionAnalytics(
        Guid tenantId, 
        [FromQuery] DateTime? fromDate = null, 
        [FromQuery] DateTime? toDate = null)
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
    [HttpGet("audit-trail")]
    [ProducesResponseType(typeof(PermissionAuditTrailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionAuditTrailDto>> GetPermissionAuditTrail(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = new GetPermissionAuditTrailQuery 
            { 
                UserId = userId, 
                TenantId = tenantId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission audit trail for user {UserId}", userId);

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Get cache statistics for permission system performance monitoring
    /// </summary>
    [HttpGet("cache/stats")]
    [ProducesResponseType(typeof(PermissionCacheStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [HttpPost("cache:clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(typeof(IEnumerable<PermissionTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [HttpPost("templates/{templateId:guid}:apply")]
    [ProducesResponseType(typeof(ApplyPermissionTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApplyPermissionTemplateResult>> ApplyPermissionTemplate(
        Guid templateId,
        [FromBody] ApplyPermissionTemplateRequest request)
    {
        try
        {
            var command = new ApplyPermissionTemplateCommand 
            { 
                TemplateId = templateId, 
                UserId = request.UserId,
                TenantId = request.TenantId
            };
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply permission template {TemplateId} to user {UserId}", templateId, request.UserId);

            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion
}

/// <summary>
///     Request body for applying a permission template
/// </summary>
public record ApplyPermissionTemplateRequest(Guid UserId, Guid? TenantId = null);
