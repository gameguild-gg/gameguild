using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>
///     Unified security audit log viewer for administrators.
///     Provides a centralized view of all security-related audit events including:
///     - Authentication attempts (login/logout, MFA, failures)
///     - Permission audit logs (grants, revokes, changes)
///     - General audit logs (admin actions, security violations)
/// </summary>
[ApiController]
[Route("api/admin/security-audit")]
[Authorize(Roles = "Admin,SystemAdmin")]
public class SecurityAuditController(
    ISecurityAuditAggregator auditAggregator,
    IAuditService auditService,
    ILogger<SecurityAuditController> logger) : ControllerBase
{
    /// <summary>
    ///     Get unified security audit logs from all sources with filtering and pagination.
    /// </summary>
    /// <param name="request">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of unified security audit events.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UnifiedSecurityAuditResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UnifiedSecurityAuditResponse>> GetSecurityAuditLogs(
        [FromQuery] UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized("User not authenticated");

            // Log this admin access
            await auditService.LogAdminActionAsync(
                adminUserId.Value,
                "ViewSecurityAuditLogs",
                "Admin accessed unified security audit logs",
                new { Filters = request });

            var result = await auditAggregator.GetUnifiedAuditLogsAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve unified security audit logs");
            return StatusCode(500, "Failed to retrieve security audit logs");
        }
    }

    /// <summary>
    ///     Get authentication attempt logs specifically.
    /// </summary>
    [HttpGet("authentication")]
    [ProducesResponseType(typeof(AuthenticationAuditResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationAuditResponse>> GetAuthenticationLogs(
        [FromQuery] AuthenticationAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized("User not authenticated");

            await auditService.LogAdminActionAsync(
                adminUserId.Value,
                "ViewAuthenticationLogs",
                "Admin accessed authentication audit logs");

            var result = await auditAggregator.GetAuthenticationLogsAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve authentication audit logs");
            return StatusCode(500, "Failed to retrieve authentication audit logs");
        }
    }

    /// <summary>
    ///     Get permission audit logs specifically.
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(PermissionAuditResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionAuditResponse>> GetPermissionLogs(
        [FromQuery] PermissionAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized("User not authenticated");

            await auditService.LogAdminActionAsync(
                adminUserId.Value,
                "ViewPermissionLogs",
                "Admin accessed permission audit logs");

            var result = await auditAggregator.GetPermissionLogsAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve permission audit logs");
            return StatusCode(500, "Failed to retrieve permission audit logs");
        }
    }

    /// <summary>
    ///     Get security audit dashboard with aggregated statistics.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(SecurityAuditDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<SecurityAuditDashboard>> GetSecurityDashboard(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized("User not authenticated");

            await auditService.LogAdminActionAsync(
                adminUserId.Value,
                "ViewSecurityDashboard",
                "Admin accessed security audit dashboard");

            var result = await auditAggregator.GetSecurityDashboardAsync(
                startDate ?? DateTime.UtcNow.AddDays(-30),
                endDate ?? DateTime.UtcNow,
                tenantId,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve security dashboard");
            return StatusCode(500, "Failed to retrieve security dashboard");
        }
    }

    /// <summary>
    ///     Export unified security audit logs to CSV.
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<ActionResult> ExportSecurityAuditLogs(
        [FromBody] UnifiedSecurityAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized("User not authenticated");

            await auditService.LogAdminActionAsync(
                adminUserId.Value,
                "ExportSecurityAuditLogs",
                "Admin exported unified security audit logs",
                new { Filters = request });

            var exportData = await auditAggregator.ExportAuditLogsAsync(request, cancellationToken);
            var fileName = $"security-audit-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";

            return File(exportData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export security audit logs");
            return StatusCode(500, "Failed to export security audit logs");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
