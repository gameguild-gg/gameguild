using System.Security.Claims;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Authentication.Controllers;

/// <summary>
/// Controller for audit log management (admin only)
/// </summary>
[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = "Admin")] // Restrict to admin users only
public class AuditController : BaseController {
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger) {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogs([FromQuery] AuditLogQueryRequest request) {
        try {
            var adminUserId = GetCurrentUserId();

            // Log admin access to audit logs
            await _auditService.LogAdminActionAsync(
              adminUserId,
              "ViewAuditLogs",
              "Admin accessed audit logs",
              new {
                  Filters = request,
                  RequestedBy = adminUserId
              });

            var query = new AuditLogQuery {
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

            var logs = await _auditService.GetAuditLogsAsync(query);
            var totalCount = await _auditService.GetAuditLogCountAsync(query);

            var response = new AuditLogResponse {
                Logs = logs.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Skip = request.Skip,
                Take = request.Take
            };

            return Ok(response);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return StatusCode(500, "Failed to retrieve audit logs");
        }
    }

    /// <summary>
    /// Get audit log statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AuditStatisticsResponse>> GetAuditStatistics([FromQuery] AuditStatisticsRequest request) {
        try {
            var adminUserId = GetCurrentUserId();

            await _auditService.LogAdminActionAsync(
              adminUserId,
              "ViewAuditStatistics",
              "Admin accessed audit statistics");

            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            // Get statistics for different categories
            var authenticationQuery = new AuditLogQuery {
                Category = AuditCategory.Authentication,
                StartDate = startDate,
                EndDate = endDate
            };

            var permissionQuery = new AuditLogQuery {
                Category = AuditCategory.Permission,
                StartDate = startDate,
                EndDate = endDate
            };

            var securityQuery = new AuditLogQuery {
                Category = AuditCategory.Security,
                StartDate = startDate,
                EndDate = endDate
            };

            var failedQuery = new AuditLogQuery {
                Success = false,
                StartDate = startDate,
                EndDate = endDate
            };

            var highRiskQuery = new AuditLogQuery {
                RiskLevel = AuditRiskLevel.High,
                StartDate = startDate,
                EndDate = endDate
            };

            var response = new AuditStatisticsResponse {
                StartDate = startDate,
                EndDate = endDate,
                TotalEvents = await _auditService.GetAuditLogCountAsync(new AuditLogQuery { StartDate = startDate, EndDate = endDate }),
                AuthenticationEvents = await _auditService.GetAuditLogCountAsync(authenticationQuery),
                PermissionEvents = await _auditService.GetAuditLogCountAsync(permissionQuery),
                SecurityEvents = await _auditService.GetAuditLogCountAsync(securityQuery),
                FailedEvents = await _auditService.GetAuditLogCountAsync(failedQuery),
                HighRiskEvents = await _auditService.GetAuditLogCountAsync(highRiskQuery)
            };

            return Ok(response);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to retrieve audit statistics");
            return StatusCode(500, "Failed to retrieve audit statistics");
        }
    }

    /// <summary>
    /// Export audit logs (admin only)
    /// </summary>
    [HttpPost("export")]
    public async Task<ActionResult> ExportAuditLogs([FromBody] AuditExportRequest request) {
        try {
            var adminUserId = GetCurrentUserId();

            await _auditService.LogAdminActionAsync(
              adminUserId,
              "ExportAuditLogs",
              "Admin exported audit logs",
              new {
                  ExportRequest = request,
                  RequestedBy = adminUserId
              });

            var query = new AuditLogQuery {
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

            var logs = await _auditService.GetAuditLogsAsync(query);

            // Convert to CSV format
            var csv = GenerateCsv(logs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            var fileName = $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to export audit logs");
            return StatusCode(500, "Failed to export audit logs");
        }
    }

    private Guid GetCurrentUserId() {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    private AuditLogDto MapToDto(AuditLog log) {
        return new AuditLogDto {
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

    private string GenerateCsv(List<AuditLog> logs) {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("Id,ActionType,ResourceType,ResourceId,UserId,TenantId,IpAddress,Description,Success,RiskLevel,Category,CreatedAt");

        // Data rows
        foreach (var log in logs) {
            csv.AppendLine($"{log.Id},{log.ActionType},{log.ResourceType},{log.ResourceId},{log.UserId},{log.TenantId},{log.IpAddress},\"{log.Description}\",{log.Success},{log.RiskLevel},{log.Category},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return csv.ToString();
    }
}

// Request/Response DTOs

public class AuditLogQueryRequest {
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? ActionType { get; set; }
    public string? ResourceType { get; set; }
    public AuditCategory? Category { get; set; }
    public AuditRiskLevel? RiskLevel { get; set; }
    public bool? Success { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? IpAddress { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
}

public class AuditLogResponse {
    public List<AuditLogDto> Logs { get; set; } = [];
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public class AuditLogDto {
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? SessionId { get; set; }
    public string? Description { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public AuditRiskLevel RiskLevel { get; set; }
    public AuditCategory Category { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditStatisticsRequest {
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class AuditStatisticsResponse {
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEvents { get; set; }
    public int AuthenticationEvents { get; set; }
    public int PermissionEvents { get; set; }
    public int SecurityEvents { get; set; }
    public int FailedEvents { get; set; }
    public int HighRiskEvents { get; set; }
}

public class AuditExportRequest {
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? ActionType { get; set; }
    public string? ResourceType { get; set; }
    public AuditCategory? Category { get; set; }
    public AuditRiskLevel? RiskLevel { get; set; }
    public bool? Success { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? IpAddress { get; set; }
}
