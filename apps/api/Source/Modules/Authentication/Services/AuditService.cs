using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Service for creating and managing audit logs
/// </summary>
public interface IAuditService {
    Task LogAsync(CreateAuditLogRequest request);
    Task LogPermissionGrantAsync(Guid userId, string permissionName, string resourceType, string? resourceId, Guid? tenantId = null);
    Task LogPermissionDenyAsync(Guid? userId, string permissionName, string resourceType, string? resourceId, string reason, Guid? tenantId = null);
    Task LogAuthenticationAsync(string actionType, Guid? userId, bool success, string? errorMessage = null);
    Task LogAdminActionAsync(Guid userId, string actionType, string description, object? metadata = null);
    Task LogSecurityViolationAsync(string violationType, string description, Guid? userId = null, object? metadata = null);
    Task<List<AuditLog>> GetAuditLogsAsync(AuditLogQuery query);
    Task<int> GetAuditLogCountAsync(AuditLogQuery query);

    // Tenant-specific audit methods
    Task LogTenantOperationAsync(string actionType, Guid tenantId, Guid? userId = null, string? description = null, object? metadata = null, bool success = true);
    Task LogTenantIsolationBypassAsync(Guid userId, string reason, object? metadata = null);

    // Role template audit methods
    Task LogRoleTemplateOperationAsync(string actionType, Guid roleTemplateId, Guid? tenantId = null, Guid? userId = null, string? description = null, object? metadata = null, bool success = true);
    Task LogTenantRoleOperationAsync(string actionType, Guid userId, Guid tenantId, string roleName, object? metadata = null, bool success = true);

    // Privacy audit methods
    Task LogPrivacyOperationAsync(string actionType, Guid userId, string? settingName = null, string? oldValue = null, string? newValue = null, Guid? tenantId = null, object? metadata = null);
    Task LogPrivacyViolationAsync(Guid? requestingUserId, Guid targetUserId, string attemptedField, string reason, Guid? tenantId = null);

    // Username normalization audit methods
    Task LogUsernameOperationAsync(string actionType, Guid userId, string? oldUsername = null, string? newUsername = null, string? reason = null, object? metadata = null);
}

public class AuditService : IAuditService {
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
      ApplicationDbContext context,
      IHttpContextAccessor httpContextAccessor,
      ILogger<AuditService> logger) {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(CreateAuditLogRequest request) {
        try {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog {
                ActionType = request.ActionType,
                ResourceType = request.ResourceType,
                ResourceId = request.ResourceId,
                UserId = request.UserId,
                TenantId = request.TenantId,
                IpAddress = request.IpAddress ?? GetClientIpAddress(httpContext),
                UserAgent = request.UserAgent ?? httpContext?.Request.Headers.UserAgent.ToString(),
                SessionId = request.SessionId ?? GetSessionId(httpContext),
                Description = request.Description,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                Success = request.Success,
                ErrorMessage = request.ErrorMessage,
                RiskLevel = request.RiskLevel,
                Category = request.Category,
                CorrelationId = request.CorrelationId ?? GetCorrelationId(httpContext)
            };

            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();

            // Log to structured logging as well for real-time monitoring
            var logLevel = GetLogLevel(request.RiskLevel, request.Success);
            _logger.Log(logLevel,
              "Audit: {ActionType} on {ResourceType} {ResourceId} by User {UserId} - {Success}",
              request.ActionType, request.ResourceType, request.ResourceId, request.UserId, request.Success ? "Success" : "Failed");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create audit log for action {ActionType}", request.ActionType);
            // Don't throw - audit logging should not break business operations
        }
    }

    public async Task LogPermissionGrantAsync(Guid userId, string permissionName, string resourceType, string? resourceId, Guid? tenantId = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = AuditActionTypes.PermissionGranted,
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            TenantId = tenantId,
            Description = $"Permission '{permissionName}' granted to user {userId}",
            Metadata = new { PermissionName = permissionName },
            Success = true,
            RiskLevel = AuditRiskLevel.Medium,
            Category = AuditCategory.Permission
        });
    }

    public async Task LogPermissionDenyAsync(Guid? userId, string permissionName, string resourceType, string? resourceId, string reason, Guid? tenantId = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = AuditActionTypes.PermissionDenied,
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            TenantId = tenantId,
            Description = $"Permission '{permissionName}' denied: {reason}",
            Metadata = new { PermissionName = permissionName, Reason = reason },
            Success = false,
            RiskLevel = AuditRiskLevel.Medium,
            Category = AuditCategory.Permission
        });
    }

    public async Task LogAuthenticationAsync(string actionType, Guid? userId, bool success, string? errorMessage = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "User",
            ResourceId = userId?.ToString(),
            UserId = userId,
            Description = $"Authentication {actionType}: {(success ? "Success" : "Failed")}",
            Success = success,
            ErrorMessage = errorMessage,
            RiskLevel = success ? AuditRiskLevel.Low : AuditRiskLevel.High,
            Category = AuditCategory.Authentication
        });
    }

    public async Task LogAdminActionAsync(Guid userId, string actionType, string description, object? metadata = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "System",
            UserId = userId,
            Description = description,
            Metadata = metadata,
            Success = true,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Admin
        });
    }

    public async Task LogSecurityViolationAsync(string violationType, string description, Guid? userId = null, object? metadata = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = AuditActionTypes.SecurityViolation,
            ResourceType = "Security",
            UserId = userId,
            Description = $"{violationType}: {description}",
            Metadata = metadata,
            Success = false,
            RiskLevel = AuditRiskLevel.Critical,
            Category = AuditCategory.Security
        });
    }

    public async Task LogTenantOperationAsync(string actionType, Guid tenantId, Guid? userId = null, string? description = null, object? metadata = null, bool success = true) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "Tenant",
            ResourceId = tenantId.ToString(),
            UserId = userId,
            TenantId = tenantId,
            Description = description ?? $"Tenant operation: {actionType}",
            Metadata = metadata,
            Success = success,
            RiskLevel = AuditRiskLevel.Medium,
            Category = AuditCategory.Tenant
        });
    }

    public async Task LogTenantIsolationBypassAsync(Guid userId, string reason, object? metadata = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = AuditActionTypes.TenantIsolationBypassed,
            ResourceType = "System",
            UserId = userId,
            Description = $"Tenant isolation bypassed: {reason}",
            Metadata = metadata,
            Success = true,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Security
        });
    }

    public async Task LogRoleTemplateOperationAsync(string actionType, Guid roleTemplateId, Guid? tenantId = null, Guid? userId = null, string? description = null, object? metadata = null, bool success = true) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "RoleTemplate",
            ResourceId = roleTemplateId.ToString(),
            UserId = userId,
            TenantId = tenantId,
            Description = description ?? $"Role template operation: {actionType}",
            Metadata = metadata,
            Success = success,
            RiskLevel = AuditRiskLevel.Medium,
            Category = AuditCategory.RoleTemplate
        });
    }

    public async Task LogTenantRoleOperationAsync(string actionType, Guid userId, Guid tenantId, string roleName, object? metadata = null, bool success = true) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "TenantRole",
            ResourceId = $"{tenantId}:{roleName}",
            UserId = userId,
            TenantId = tenantId,
            Description = $"Tenant role {actionType}: {roleName} for user {userId}",
            Metadata = new { RoleName = roleName, AdditionalMetadata = metadata },
            Success = success,
            RiskLevel = AuditRiskLevel.Medium,
            Category = AuditCategory.RoleTemplate
        });
    }

    public async Task LogPrivacyOperationAsync(string actionType, Guid userId, string? settingName = null, string? oldValue = null, string? newValue = null, Guid? tenantId = null, object? metadata = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "UserPrivacy",
            ResourceId = userId.ToString(),
            UserId = userId,
            TenantId = tenantId,
            Description = settingName != null
                ? $"Privacy setting '{settingName}' changed from '{oldValue}' to '{newValue}'"
                : $"Privacy operation: {actionType}",
            Metadata = new { SettingName = settingName, OldValue = oldValue, NewValue = newValue, AdditionalMetadata = metadata },
            Success = true,
            RiskLevel = AuditRiskLevel.Low,
            Category = AuditCategory.Privacy
        });
    }

    public async Task LogPrivacyViolationAsync(Guid? requestingUserId, Guid targetUserId, string attemptedField, string reason, Guid? tenantId = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = AuditActionTypes.PrivacyViolationAttempt,
            ResourceType = "UserPrivacy",
            ResourceId = targetUserId.ToString(),
            UserId = requestingUserId,
            TenantId = tenantId,
            Description = $"Privacy violation attempt: {reason} - attempted to access '{attemptedField}' of user {targetUserId}",
            Metadata = new { TargetUserId = targetUserId, AttemptedField = attemptedField, Reason = reason },
            Success = false,
            RiskLevel = AuditRiskLevel.High,
            Category = AuditCategory.Privacy
        });
    }

    public async Task LogUsernameOperationAsync(string actionType, Guid userId, string? oldUsername = null, string? newUsername = null, string? reason = null, object? metadata = null) {
        await LogAsync(new CreateAuditLogRequest {
            ActionType = actionType,
            ResourceType = "User",
            ResourceId = userId.ToString(),
            UserId = userId,
            Description = oldUsername != null && newUsername != null
                ? $"Username changed from '{oldUsername}' to '{newUsername}'{(reason != null ? $" - Reason: {reason}" : "")}"
                : $"Username operation: {actionType}{(reason != null ? $" - Reason: {reason}" : "")}",
            Metadata = new { OldUsername = oldUsername, NewUsername = newUsername, Reason = reason, AdditionalMetadata = metadata },
            Success = true,
            RiskLevel = AuditRiskLevel.Low,
            Category = AuditCategory.User
        });
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(AuditLogQuery query) {
        var queryable = _context.Set<AuditLog>().AsQueryable();

        // Apply filters
        if (query.UserId.HasValue) {
            queryable = queryable.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.TenantId.HasValue) {
            queryable = queryable.Where(a => a.TenantId == query.TenantId.Value);
        }

        if (!string.IsNullOrEmpty(query.ActionType)) {
            queryable = queryable.Where(a => a.ActionType == query.ActionType);
        }

        if (!string.IsNullOrEmpty(query.ResourceType)) {
            queryable = queryable.Where(a => a.ResourceType == query.ResourceType);
        }

        if (query.Category.HasValue) {
            queryable = queryable.Where(a => a.Category == query.Category.Value);
        }

        if (query.RiskLevel.HasValue) {
            queryable = queryable.Where(a => a.RiskLevel >= query.RiskLevel.Value);
        }

        if (query.Success.HasValue) {
            queryable = queryable.Where(a => a.Success == query.Success.Value);
        }

        if (query.StartDate.HasValue) {
            queryable = queryable.Where(a => a.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue) {
            queryable = queryable.Where(a => a.CreatedAt <= query.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(query.IpAddress)) {
            queryable = queryable.Where(a => a.IpAddress == query.IpAddress);
        }

        // Apply ordering
        queryable = queryable.OrderByDescending(a => a.CreatedAt);

        // Apply pagination
        if (query.Skip > 0) {
            queryable = queryable.Skip(query.Skip);
        }

        if (query.Take > 0) {
            queryable = queryable.Take(Math.Min(query.Take, 1000)); // Cap at 1000 records
        }

        return await queryable.ToListAsync();
    }

    public async Task<int> GetAuditLogCountAsync(AuditLogQuery query) {
        var queryable = _context.Set<AuditLog>().AsQueryable();

        // Apply same filters as GetAuditLogsAsync but without ordering/pagination
        if (query.UserId.HasValue) {
            queryable = queryable.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.TenantId.HasValue) {
            queryable = queryable.Where(a => a.TenantId == query.TenantId.Value);
        }

        if (!string.IsNullOrEmpty(query.ActionType)) {
            queryable = queryable.Where(a => a.ActionType == query.ActionType);
        }

        if (!string.IsNullOrEmpty(query.ResourceType)) {
            queryable = queryable.Where(a => a.ResourceType == query.ResourceType);
        }

        if (query.Category.HasValue) {
            queryable = queryable.Where(a => a.Category == query.Category.Value);
        }

        if (query.RiskLevel.HasValue) {
            queryable = queryable.Where(a => a.RiskLevel >= query.RiskLevel.Value);
        }

        if (query.Success.HasValue) {
            queryable = queryable.Where(a => a.Success == query.Success.Value);
        }

        if (query.StartDate.HasValue) {
            queryable = queryable.Where(a => a.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue) {
            queryable = queryable.Where(a => a.CreatedAt <= query.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(query.IpAddress)) {
            queryable = queryable.Where(a => a.IpAddress == query.IpAddress);
        }

        return await queryable.CountAsync();
    }

    private string? GetClientIpAddress(HttpContext? httpContext) {
        if (httpContext == null) return null;

        // Check for X-Forwarded-For header (reverse proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for X-Real-IP header (Nginx)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) {
            return realIp;
        }

        // Fallback to remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetSessionId(HttpContext? httpContext) {
        if (httpContext?.User?.FindFirst("session_id")?.Value is string sessionIdStr &&
            Guid.TryParse(sessionIdStr, out var sessionId)) {
            return sessionId;
        }
        return null;
    }

    private string? GetCorrelationId(HttpContext? httpContext) {
        return httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    }

    private LogLevel GetLogLevel(AuditRiskLevel riskLevel, bool success) {
        return riskLevel switch {
            AuditRiskLevel.Critical => LogLevel.Critical,
            AuditRiskLevel.High => success ? LogLevel.Warning : LogLevel.Error,
            AuditRiskLevel.Medium => LogLevel.Information,
            AuditRiskLevel.Low => LogLevel.Debug,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Query parameters for retrieving audit logs
/// </summary>
public class AuditLogQuery {
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
