using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Abstraction for sending SLA violation notifications.
///     Implemented by the Users module to send actual notifications.
/// </summary>
public interface ISlaNotificationSender
{
    /// <summary>
    ///     Send a violation notification to a specific user.
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string priority,
        string? actionUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Send a violation alert via webhook.
    /// </summary>
    Task SendWebhookAsync(
        string webhookUrl,
        object payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default implementation that logs notifications (used when no sender is registered).
/// </summary>
public class LoggingSlaNotificationSender(ILogger<LoggingSlaNotificationSender> logger) : ISlaNotificationSender
{
    public Task SendToUserAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string priority,
        string? actionUrl,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "SLA Notification to user {UserId}: [{Priority}] {Title} - {Message}",
            userId, priority, title, message);
        return Task.CompletedTask;
    }

    public Task SendWebhookAsync(
        string webhookUrl,
        object payload,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "SLA Webhook notification to {Url}: {Payload}",
            webhookUrl, payload);
        return Task.CompletedTask;
    }
}

/// <summary>
///     Implementation of SLA incident escalation service.
///     Wires SLA violations to the notification system and incident management.
///     Uses Lazy&lt;ISlaImpactAnalysisService&gt; to break circular dependency.
/// </summary>
public class SlaIncidentEscalationService(
    Lazy<ISlaImpactAnalysisService> slaServiceLazy,
    ISlaImpactAnalysisRepository slaRepository,
    ISlaNotificationSender notificationSender,
    ILogger<SlaIncidentEscalationService> logger
) : ISlaIncidentEscalationService
{
    private ISlaImpactAnalysisService SlaService => slaServiceLazy.Value;
    
    // In-memory config cache - in production would use a repository
    private static readonly Dictionary<Guid, SlaEscalationConfig> ConfigCache = new();
    private static readonly object ConfigLock = new();

    public async Task<SlaEscalationResult> EscalateViolationAsync(
        SlaImpactAnalysis violation,
        CancellationToken cancellationToken = default)
    {
        if (violation.TenantId == null)
        {
            logger.LogWarning("Cannot escalate violation {ViolationId} - no tenant ID", violation.Id);
            return SlaEscalationResult.Failed("Violation has no tenant context");
        }

        var config = await GetEscalationConfigAsync(violation.TenantId.Value, cancellationToken);

        // Check if escalation is needed
        if (!config.AutoEscalationEnabled)
        {
            logger.LogDebug("Auto-escalation disabled for tenant {TenantId}", violation.TenantId);
            return SlaEscalationResult.NotRequired();
        }

        if (violation.Severity < config.MinimumEscalationSeverity)
        {
            logger.LogDebug(
                "Violation {ViolationId} severity {Severity} below threshold {Threshold}",
                violation.Id, violation.Severity, config.MinimumEscalationSeverity);
            return SlaEscalationResult.NotRequired();
        }

        string? incidentId = null;
        var notifiedUsers = new List<Guid>();

        try
        {
            // Create incident ticket if configured
            if (config.AutoCreateIncidents && !violation.IncidentCreated)
            {
                incidentId = await SlaService.CreateIncidentTicketAsync(violation.Id, cancellationToken);
                
                logger.LogInformation(
                    "Created incident {IncidentId} for violation {ViolationId}",
                    incidentId, violation.Id);
            }

            // Send notifications
            await SendViolationNotificationAsync(violation, cancellationToken);

            // Collect notified users
            notifiedUsers.AddRange(config.EscalationUserIds);

            // Mark violation as escalated
            await SlaService.UpdateViolationAsync(
                violation.Id,
                requiresEscalation: true,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Escalated violation {ViolationId}: Incident={IncidentId}, NotifiedUsers={UserCount}",
                violation.Id, incidentId, notifiedUsers.Count);

            return SlaEscalationResult.Success(incidentId, notifiedUsers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to escalate violation {ViolationId}", violation.Id);
            return SlaEscalationResult.Failed(ex.Message);
        }
    }

    public async Task<int> ProcessPendingEscalationsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing pending SLA escalations...");

        var processedCount = 0;

        // Get all tenants with unresolved high/critical violations
        // Note: In production, this would need a more efficient query
        var tenantIds = await GetTenantsWithPendingEscalationsAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var violations = await SlaService.GetUnresolvedViolationsAsync(
                tenantId,
                SlaViolationSeverity.High,
                cancellationToken);

            foreach (var violation in violations.Where(v => v.RequiresEscalation && !v.IncidentCreated))
            {
                var result = await EscalateViolationAsync(violation, cancellationToken);
                
                if (result.WasEscalated)
                {
                    processedCount++;
                }
            }
        }

        logger.LogInformation("Processed {Count} pending escalations", processedCount);
        return processedCount;
    }

    public async Task SendViolationNotificationAsync(
        SlaImpactAnalysis violation,
        CancellationToken cancellationToken = default)
    {
        if (violation.TenantId == null)
        {
            logger.LogWarning("Cannot send notification for violation {ViolationId} - no tenant", violation.Id);
            return;
        }

        var config = await GetEscalationConfigAsync(violation.TenantId.Value, cancellationToken);

        var severityText = violation.Severity switch
        {
            SlaViolationSeverity.Critical => "🚨 CRITICAL",
            SlaViolationSeverity.High => "⚠️ HIGH",
            SlaViolationSeverity.Medium => "📊 MEDIUM",
            _ => "ℹ️ LOW"
        };

        var notificationTitle = $"{severityText} SLA Violation: {violation.ViolationType}";
        var notificationBody = $"Resource quota violation detected.\n" +
                               $"Expected: {violation.ExpectedValue}, Actual: {violation.ActualValue}\n" +
                               $"Deviation: {violation.DeviationPercentage:P2}";

        // Send to configured users
        foreach (var userId in config.EscalationUserIds)
        {
            await notificationSender.SendToUserAsync(
                userId: userId,
                title: notificationTitle,
                message: notificationBody,
                type: "SlaViolation",
                priority: violation.Severity >= SlaViolationSeverity.High ? "high" : "normal",
                actionUrl: $"/admin/sla/violations/{violation.Id}",
                cancellationToken: cancellationToken);
        }

        // Send webhook if configured
        if (!string.IsNullOrEmpty(config.WebhookUrl))
        {
            await notificationSender.SendWebhookAsync(
                config.WebhookUrl,
                new
                {
                    ViolationId = violation.Id,
                    violation.TenantId,
                    violation.ViolationType,
                    violation.Severity,
                    violation.ExpectedValue,
                    violation.ActualValue,
                    violation.DeviationPercentage,
                    Timestamp = DateTime.UtcNow
                },
                cancellationToken);
        }

        logger.LogInformation(
            "Sent {Count} notifications for violation {ViolationId}",
            config.EscalationUserIds.Count, violation.Id);
    }

    public Task<SlaEscalationConfig> GetEscalationConfigAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        lock (ConfigLock)
        {
            if (ConfigCache.TryGetValue(tenantId, out var config))
            {
                return Task.FromResult(config);
            }

            // Return default config
            return Task.FromResult(new SlaEscalationConfig { TenantId = tenantId });
        }
    }

    public Task<SlaEscalationConfig> SetEscalationConfigAsync(
        Guid tenantId,
        SlaEscalationConfig config,
        CancellationToken cancellationToken = default)
    {
        var configWithTenant = config with { TenantId = tenantId };

        lock (ConfigLock)
        {
            ConfigCache[tenantId] = configWithTenant;
        }

        logger.LogInformation(
            "Updated escalation config for tenant {TenantId}: AutoEscalation={Enabled}, MinSeverity={Severity}",
            tenantId, config.AutoEscalationEnabled, config.MinimumEscalationSeverity);

        return Task.FromResult(configWithTenant);
    }

    private async Task<List<Guid>> GetTenantsWithPendingEscalationsAsync(CancellationToken cancellationToken)
    {
        // Get distinct tenant IDs from unresolved violations
        // This is a simplified implementation - in production would use a dedicated query
        var allViolations = await slaRepository.GetUnresolvedAsync(Guid.Empty, cancellationToken);
        
        return allViolations
            .Where(v => v.TenantId.HasValue && v.RequiresEscalation && !v.IncidentCreated)
            .Select(v => v.TenantId!.Value)
            .Distinct()
            .ToList();
    }

}
