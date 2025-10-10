using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Commands;
using GameGuild.Modules.Permissions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Permissions.Controllers;

/// <summary>
/// API controller for permission management operations
/// Enhanced with CQRS pattern, caching, and audit logging
/// </summary>
[ApiController]
[Route("api/permissions")]
[Tags("Permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IMediator mediator, ILogger<PermissionsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Grant tenant permissions to a user
    /// </summary>
    [HttpPost("tenant/grant")]
    public async Task<ActionResult<TenantPermission>> GrantTenantPermission(
        [FromBody] GrantTenantPermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant tenant permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke tenant permissions from a user
    /// </summary>
    [HttpPost("tenant/revoke")]
    public async Task<ActionResult> RevokeTenantPermission(
        [FromBody] RevokeTenantPermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tenant permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if user has a specific tenant permission
    /// </summary>
    [HttpPost("tenant/check")]
    public async Task<ActionResult<bool>> HasTenantPermission(
        [FromBody] HasTenantPermissionQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check tenant permission");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all tenant permissions for a user
    /// </summary>
    [HttpPost("tenant/list")]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetTenantPermissions(
        [FromBody] GetTenantPermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Grant resource-level permissions to a user
    /// </summary>
    [HttpPost("resource/grant")]
    public async Task<ActionResult<ResourcePermission>> GrantResourcePermission(
        [FromBody] GrantResourcePermissionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant resource permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke resource-level permissions from a user
    /// </summary>
    [HttpPost("resource/revoke")]
    public async Task<ActionResult> RevokeResourcePermission(
        [FromBody] RevokeResourcePermissionCommand command)
    {
        try
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke resource permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if user has a specific resource permission
    /// </summary>
    [HttpPost("resource/check")]
    public async Task<ActionResult<bool>> HasResourcePermission(
        [FromBody] HasResourcePermissionQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resource permission");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all resource permissions for a user
    /// </summary>
    [HttpPost("resource/list")]
    public async Task<ActionResult<IEnumerable<PermissionType>>> GetResourcePermissions(
        [FromBody] GetResourcePermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all permissions for a user (tenant + resource)
    /// </summary>
    [HttpPost("user/all")]
    public async Task<ActionResult<UserPermissionsDto>> GetUserPermissions(
        [FromBody] GetUserPermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get effective permissions for a user (resolved through all layers)
    /// </summary>
    [HttpPost("user/effective")]
    public async Task<ActionResult<EffectivePermissionsDto>> GetEffectivePermissions(
        [FromBody] GetEffectivePermissionsQuery query)
    {
        try
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effective permissions");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get permission usage analytics for a tenant
    /// </summary>
    [HttpGet("analytics/{tenantId}")]
    public Task<ActionResult> GetPermissionAnalytics(Guid tenantId)
    {
        try
        {
            // This would be implemented as a query using the analytics service
            return Task.FromResult<ActionResult>(Ok(new { message = $"Analytics endpoint ready for implementation for tenant {tenantId}" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission analytics for tenant {TenantId}", tenantId);
            return Task.FromResult<ActionResult>(BadRequest(new { error = ex.Message }));
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/stats")]
    public Task<ActionResult> GetCacheStatistics()
    {
        try
        {
            // This would get cache statistics from the cached permission service
            return Task.FromResult<ActionResult>(Ok(new { message = "Cache statistics endpoint ready for implementation" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return Task.FromResult<ActionResult>(BadRequest(new { error = ex.Message }));
        }
    }
}