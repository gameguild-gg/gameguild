using System.Text.Json;

namespace GameGuild.Core.Services;

/// <summary>
/// Service for immutable audit event logging
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(string eventType, string description, Guid? userId = null, Guid? tenantId = null, object? metadata = null);

    /// <summary>
    /// Logs a data change event
    /// </summary>
    Task LogDataChangeAsync(string entity, string action, Guid entityId, Guid? userId = null, Guid? tenantId = null, object? before = null, object? after = null);

    /// <summary>
    /// Logs a user action
    /// </summary>
    Task LogUserActionAsync(string action, string description, Guid userId, Guid? tenantId = null, object? metadata = null);

    /// <summary>
    /// Logs a performance event
    /// </summary>
    Task LogPerformanceEventAsync(string operationName, TimeSpan duration, bool success, string? errorMessage = null);
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public Task LogSecurityEventAsync(string eventType, string description, Guid? userId = null, Guid? tenantId = null, object? metadata = null)
    {
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "Security",
            Category = eventType,
            Description = description,
            UserId = userId,
            TenantId = tenantId,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };

        _logger.LogWarning(
            "[AUDIT] Security Event: {EventType} - {Description} | User: {UserId} | Tenant: {TenantId} | Metadata: {Metadata}",
            eventType, description, userId, tenantId, auditEvent.Metadata);

        return Task.CompletedTask;
    }

    public Task LogDataChangeAsync(string entity, string action, Guid entityId, Guid? userId = null, Guid? tenantId = null, object? before = null, object? after = null)
    {
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "DataChange",
            Category = $"{entity}.{action}",
            Description = $"{action} on {entity}",
            UserId = userId,
            TenantId = tenantId,
            EntityType = entity,
            EntityId = entityId,
            BeforeState = before != null ? JsonSerializer.Serialize(before) : null,
            AfterState = after != null ? JsonSerializer.Serialize(after) : null
        };

        _logger.LogInformation(
            "[AUDIT] Data Change: {Entity}.{Action} | EntityId: {EntityId} | User: {UserId} | Tenant: {TenantId}",
            entity, action, entityId, userId, tenantId);

        return Task.CompletedTask;
    }

    public Task LogUserActionAsync(string action, string description, Guid userId, Guid? tenantId = null, object? metadata = null)
    {
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "UserAction",
            Category = action,
            Description = description,
            UserId = userId,
            TenantId = tenantId,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };

        _logger.LogInformation(
            "[AUDIT] User Action: {Action} - {Description} | User: {UserId} | Tenant: {TenantId} | Metadata: {Metadata}",
            action, description, userId, tenantId, auditEvent.Metadata);

        return Task.CompletedTask;
    }

    public Task LogPerformanceEventAsync(string operationName, TimeSpan duration, bool success, string? errorMessage = null)
    {
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = "Performance",
            Category = operationName,
            Description = $"Operation completed in {duration.TotalMilliseconds:F2}ms",
            Success = success,
            ErrorMessage = errorMessage,
            DurationMs = duration.TotalMilliseconds
        };

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(logLevel,
            "[AUDIT] Performance: {Operation} | Duration: {Duration}ms | Success: {Success} | Error: {Error}",
            operationName, duration.TotalMilliseconds, success, errorMessage);

        return Task.CompletedTask;
    }
}

public class AuditEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? Metadata { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public double? DurationMs { get; set; }
}
