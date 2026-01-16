using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Controller for audit log management (admin only)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/audit-logs")]
[Authorize(Roles = "Admin")] // Restrict to admin users only
public class AuditController(IAuditService auditService, ILogger<AuditController> logger) : ControllerBase
{
    /// <summary>
    /// Gets the current user ID from the JWT claims
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        
        return userId;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogs([FromQuery] AuditLogQueryRequest request)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

            // Log admin access to audit logs
            await auditService.LogAdminActionAsync(adminUserId.Value, "ViewAuditLogs", "Admin accessed audit logs", new { Filters = request, RequestedBy = adminUserId.Value });

            var query = new AuditLogQuery
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                ActionType = request.ActionType,
                ResourceType = request.ResourceType,
                Category = request.Category,
                RiskLevel = request.RiskLevel,
                Success = request.Success,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IpAddress = request.IpAddress,
                Skip = request.Skip,
                Take = Math.Min(request.Take, 1000) // Cap at 1000 records
            };

            var logs = await auditService.GetAuditLogsAsync(query);
            var totalCount = await auditService.GetAuditLogCountAsync(query);

            var response = new AuditLogResponse { Logs = logs.Select(MapToDto).ToList(), TotalCount = totalCount, Skip = request.Skip, Take = request.Take };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve audit logs");

            return StatusCode(500, "Failed to retrieve audit logs");
        }
    }

    /// <summary>
    /// Get audit log statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AuditStatisticsResponse>> GetAuditStatistics([FromQuery] AuditStatisticsRequest request)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

            await auditService.LogAdminActionAsync(adminUserId.Value, "ViewAuditStatistics", "Admin accessed audit statistics");

            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            // Get statistics for different categories
            var authenticationQuery = new AuditLogQuery { Category = AuditCategory.Authentication, StartDate = startDate, EndDate = endDate };

            var permissionQuery = new AuditLogQuery { Category = AuditCategory.Permission, StartDate = startDate, EndDate = endDate };

            var securityQuery = new AuditLogQuery { Category = AuditCategory.Security, StartDate = startDate, EndDate = endDate };

            var failedQuery = new AuditLogQuery { Success = false, StartDate = startDate, EndDate = endDate };

            var highRiskQuery = new AuditLogQuery { RiskLevel = AuditRiskLevel.High, StartDate = startDate, EndDate = endDate };

            var response = new AuditStatisticsResponse
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalEvents = await auditService.GetAuditLogCountAsync(new AuditLogQuery { StartDate = startDate, EndDate = endDate }),
                AuthenticationEvents = await auditService.GetAuditLogCountAsync(authenticationQuery),
                PermissionEvents = await auditService.GetAuditLogCountAsync(permissionQuery),
                SecurityEvents = await auditService.GetAuditLogCountAsync(securityQuery),
                FailedEvents = await auditService.GetAuditLogCountAsync(failedQuery),
                HighRiskEvents = await auditService.GetAuditLogCountAsync(highRiskQuery)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve audit statistics");

            return StatusCode(500, "Failed to retrieve audit statistics");
        }
    }

    /// <summary>
    /// Export audit logs (admin only)
    /// </summary>
    [HttpPost(":export")]
    public async Task<ActionResult> ExportAuditLogs([FromBody] AuditExportRequest request)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

            await auditService.LogAdminActionAsync(adminUserId.Value, "ExportAuditLogs", "Admin exported audit logs", new { ExportRequest = request, RequestedBy = adminUserId.Value });

            var query = new AuditLogQuery
            {
                UserId = request.UserId,
                TenantId = request.TenantId,
                ActionType = request.ActionType,
                ResourceType = request.ResourceType,
                Category = request.Category,
                RiskLevel = request.RiskLevel,
                Success = request.Success,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IpAddress = request.IpAddress,
                Take = 0 // Get all matching records for export
            };

            var logs = await auditService.GetAuditLogsAsync(query);

            // Convert to CSV format
            var csv = GenerateCsv(logs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            var fileName = $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export audit logs");

            return StatusCode(500, "Failed to export audit logs");
        }
    }

    private AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            ActionType = log.ActionType,
            ResourceType = log.ResourceType,
            ResourceId = log.ResourceId,
            UserId = log.UserId,
            TenantId = log.TenantId,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            SessionId = log.SessionId,
            Description = log.Description,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage,
            RiskLevel = log.RiskLevel,
            Category = log.Category,
            CorrelationId = log.CorrelationId,
            CreatedAt = log.CreatedAt
        };
    }

    private string GenerateCsv(List<AuditLog> logs)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("Id,ActionType,ResourceType,ResourceId,UserId,TenantId,IpAddress,Description,Success,RiskLevel,Category,CreatedAt");

        // Data rows
        foreach (var log in logs)
        {
            csv.AppendLine(
                $"{log.Id},{log.ActionType},{log.ResourceType},{log.ResourceId},{log.UserId},{log.TenantId},{log.IpAddress},\"{log.Description}\",{log.Success},{log.RiskLevel},{log.Category},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}"
            );
        }

        return csv.ToString();
    }
}

// Request/Response DTOs
