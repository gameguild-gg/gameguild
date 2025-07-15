using GameGuild.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Examples.Controllers;

/// <summary>
/// Example controller demonstrating how to use IUserContext and ITenantContext
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContextExampleController : ControllerBase
{
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ContextExampleController> _logger;

    public ContextExampleController(
        IUserContext userContext,
        ITenantContext tenantContext,
        ILogger<ContextExampleController> logger)
    {
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Example endpoint showing how to access user context
    /// </summary>
    [HttpGet("user-info")]
    public IActionResult GetUserInfo()
    {
        if (!_userContext.IsAuthenticated)
        {
            return Unauthorized("User is not authenticated");
        }

        var userInfo = new
        {
            UserId = _userContext.UserId,
            Email = _userContext.Email,
            Name = _userContext.Name,
            Roles = _userContext.Roles,
            IsAuthenticated = _userContext.IsAuthenticated
        };

        _logger.LogInformation("User info requested by {UserId}", _userContext.UserId);

        return Ok(userInfo);
    }

    /// <summary>
    /// Example endpoint showing how to access tenant context
    /// </summary>
    [HttpGet("tenant-info")]
    public IActionResult GetTenantInfo()
    {
        var tenantInfo = new
        {
            TenantId = _tenantContext.TenantId,
            TenantName = _tenantContext.TenantName,
            IsActive = _tenantContext.IsActive,
            SubscriptionPlan = _tenantContext.SubscriptionPlan,
            Settings = _tenantContext.Settings
        };

        _logger.LogInformation("Tenant info requested for tenant {TenantId} by user {UserId}", 
            _tenantContext.TenantId, _userContext.UserId);

        return Ok(tenantInfo);
    }

    /// <summary>
    /// Example endpoint showing role-based access control using context
    /// </summary>
    [HttpGet("admin-only")]
    public IActionResult GetAdminOnlyData()
    {
        if (!_userContext.IsInRole("Admin"))
        {
            return Forbid("Admin access required");
        }

        var adminData = new
        {
            Message = "This is admin-only data",
            AccessedBy = _userContext.UserId,
            AccessedAt = DateTime.UtcNow,
            TenantId = _tenantContext.TenantId
        };

        _logger.LogInformation("Admin data accessed by {UserId} in tenant {TenantId}", 
            _userContext.UserId, _tenantContext.TenantId);

        return Ok(adminData);
    }

    /// <summary>
    /// Example showing how to work with tenant-specific data
    /// </summary>
    [HttpGet("tenant-specific-data")]
    public IActionResult GetTenantSpecificData()
    {
        if (_tenantContext.TenantId == null)
        {
            return BadRequest("No tenant context available");
        }

        if (!_tenantContext.IsActive)
        {
            return BadRequest("Tenant is not active");
        }

        var data = new
        {
            TenantId = _tenantContext.TenantId,
            Message = $"Data specific to tenant: {_tenantContext.TenantName}",
            SubscriptionPlan = _tenantContext.SubscriptionPlan,
            AccessedBy = _userContext.UserId,
            UserRoles = _userContext.Roles
        };

        return Ok(data);
    }
}
