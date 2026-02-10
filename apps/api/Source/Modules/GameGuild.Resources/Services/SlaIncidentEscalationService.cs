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
///     Depends on <see cref="ISlaImpactAnalysisRepository"/> and <see cref="IIncidentTicketProvider"/>
///     directly to avoid a circular dependency with <c>ISlaImpactAnalysisService</c>.
/// </summary>
public class SlaIncidentEscalationService(
    ISlaImpactAnalysisRepository slaRepository,
    IIncidentTicketProvider incidentTicketProvider,
    ISlaNotificationSender notificationSender,
    ILogger<SlaIncidentEscalationService> logger
) : ISlaIncidentEscalationService
{
    
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

        var config = await GetEscalationConfigAsync(violation.TenantId.Value, cancellationToken).ConfigureAwait(false);

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
                incidentId = await incidentTicketProvider.CreateTicketAsync(violation, cancellationToken).ConfigureAwait(false);

                // Update the violation record directly via the repository
                violation.IncidentCreated = true;
                violation.IncidentTicketId = incidentId;
                await slaRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);
                
                logger.LogInformation(
                    "Created incident {IncidentId} for violation {ViolationId}",
                    incidentId, violation.Id);
            }

            // Send notifications
            await SendViolationNotificationAsync(violation, cancellationToken).ConfigureAwait(false);

            // Collect notified users
            notifiedUsers.AddRange(config.EscalationUserIds);

            // Mark violation as escalated directly via the repository
            violation.RequiresEscalation = true;
            await slaRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

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
        var tenantIds = await GetTenantsWithPendingEscalationsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tenantId in tenantIds)
        {
            var allViolations = await slaRepository.GetUnresolvedAsync(tenantId, cancellationToken).ConfigureAwait(false);
            var violations = allViolations
                .Where(v => v.Severity >= SlaViolationSeverity.High);

            foreach (var violation in violations.Where(v => v.RequiresEscalation && !v.IncidentCreated))
            {
                var result = await EscalateViolationAsync(violation, cancellationToken).ConfigureAwait(false);
                
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

        var config = await GetEscalationConfigAsync(violation.TenantId.Value, cancellationToken).ConfigureAwait(false);

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
                    Timestamp = SystemClock.UtcNow
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
        var allViolations = await slaRepository.GetUnresolvedAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        
        return allViolations
            .Where(v => v.TenantId.HasValue && v.RequiresEscalation && !v.IncidentCreated)
            .Select(v => v.TenantId!.Value)
            .Distinct()
            .ToList();
    }

}
