using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for auditing permission operations
/// </summary>
public class PermissionAuditService : IPermissionAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionAuditService> _logger;

    public PermissionAuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionAuditService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogPermissionGrantedAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        string operation,
        PermissionType[] permissions,
        string? reason = null,
        string? contentTypeName = null,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var auditLog = CreateAuditLogEntry(
                userId, tenantId, resourceId, operation, permissions, reason, contentTypeName, metadata, true);

            _context.PermissionAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logged permission audit: {Operation} for User:{UserId} in Tenant:{TenantId}",
                operation, userId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission audit for User:{UserId} in Tenant:{TenantId}",
                userId, tenantId);
            // Don't throw - audit logging shouldn't break the main operation
        }
    }

    public async Task LogPermissionCheckAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission,
        bool hasPermission,
        string? contentTypeName = null)
    {
        try
        {
            var auditLog = CreateAuditLogEntry(
                userId, tenantId, resourceId, "Check", new[] { permission },
                hasPermission ? "Granted" : "Denied", contentTypeName, null, hasPermission);

            _context.PermissionAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Logged permission check: {Permission} = {HasPermission} for User:{UserId} in Tenant:{TenantId}",
                permission, hasPermission, userId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log permission check for User:{UserId} in Tenant:{TenantId}",
                userId, tenantId);
        }
    }

    public async Task LogPermissionDeniedAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission,
        string? reason = null,
        string? contentTypeName = null)
    {
        try
        {
            var auditLog = CreateAuditLogEntry(
                userId, tenantId, resourceId, "Denied", new[] { permission },
                reason ?? "Access denied", contentTypeName, null, false);

            _context.PermissionAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Logged permission denied: {Permission} for User:{UserId} in Tenant:{TenantId}, Reason:{Reason}",
                permission, userId, tenantId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission denied for User:{UserId} in Tenant:{TenantId}",
                userId, tenantId);
        }
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetUserAuditLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100)
    {
        var query = _context.PermissionAuditLogs
            .Where(log => log.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(log => log.PerformedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(log => log.PerformedAt <= toDate);

        return await query
            .OrderByDescending(log => log.PerformedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetTenantAuditLogsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100)
    {
        var query = _context.PermissionAuditLogs
            .Where(log => log.TenantId == tenantId);

        if (fromDate.HasValue)
            query = query.Where(log => log.PerformedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(log => log.PerformedAt <= toDate);

        return await query
            .OrderByDescending(log => log.PerformedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetResourceAuditLogsAsync(
        Guid resourceId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100)
    {
        var query = _context.PermissionAuditLogs
            .Where(log => log.ResourceId == resourceId);

        if (fromDate.HasValue)
            query = query.Where(log => log.PerformedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(log => log.PerformedAt <= toDate);

        return await query
            .OrderByDescending(log => log.PerformedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetFailedPermissionAttemptsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100)
    {
        var query = _context.PermissionAuditLogs
            .Where(log => !log.IsSuccess || log.Operation == "Denied");

        if (fromDate.HasValue)
            query = query.Where(log => log.PerformedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(log => log.PerformedAt <= toDate);

        return await query
            .OrderByDescending(log => log.PerformedAt)
            .Take(limit)
            .ToListAsync();
    }

    private PermissionAuditLog CreateAuditLogEntry(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        string operation,
        PermissionType[] permissions,
        string? reason,
        string? contentTypeName,
        Dictionary<string, object>? metadata,
        bool isSuccess)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        return new PermissionAuditLog
        {
            UserId = userId,
            TenantId = tenantId,
            ResourceId = resourceId,
            Operation = operation,
            Permissions = permissions,
            Reason = reason,
            PerformedBy = GetCurrentUserId(),
            PerformedAt = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            ContentTypeName = contentTypeName,
            Metadata = metadata,
            PermissionLayer = DeterminePermissionLayer(resourceId, contentTypeName),
            IsSuccess = isSuccess
        };
    }

    private Guid? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst("sub") ?? httpContext.User.FindFirst("id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        return null;
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP (some proxies use this)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string DeterminePermissionLayer(Guid? resourceId, string? contentTypeName)
    {
        if (resourceId.HasValue) return "Resource";
        if (!string.IsNullOrEmpty(contentTypeName)) return "ContentType";
        return "Tenant";
    }
}