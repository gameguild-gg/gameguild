using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Core.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase {
    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    protected Guid? GetCurrentUserId() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current tenant ID from the JWT token
    /// </summary>
    protected Guid? GetCurrentTenantId() {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    /// <summary>
    /// Gets the current user's email from the JWT token
    /// </summary>
    protected string? GetCurrentUserEmail() {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the current user's role from the JWT token
    /// </summary>
    protected string? GetCurrentUserRole() {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}