namespace GameGuild.Resources;

/// <summary>
///     Service for escalating SLA violations to incident management and notifications.
///     Bridges the gap between SLA impact analysis and the notification system.
/// </summary>
public interface ISlaIncidentEscalationService
{
    /// <summary>
    ///     Process a violation and escalate if necessary.
    ///     Creates incidents for high/critical severity violations and sends notifications.
    /// </summary>
    /// <param name="violation">The SLA violation to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Escalation result with incident ID if created</returns>
    Task<SlaEscalationResult> EscalateViolationAsync(
        SlaImpactAnalysis violation,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Process pending violations that require escalation.
    ///     Called by background job to handle unprocessed violations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of violations processed</returns>
    Task<int> ProcessPendingEscalationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Send notification for an SLA violation to tenant admins.
    /// </summary>
    /// <param name="violation">The violation to notify about</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendViolationNotificationAsync(
        SlaImpactAnalysis violation,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get escalation configuration for a tenant.
    /// </summary>
    Task<SlaEscalationConfig> GetEscalationConfigAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update escalation configuration for a tenant.
    /// </summary>
    Task<SlaEscalationConfig> SetEscalationConfigAsync(
        Guid tenantId,
        SlaEscalationConfig config,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of SLA violation escalation.
/// </summary>
public sealed record SlaEscalationResult
{
    /// <summary>Whether escalation was triggered</summary>
    public bool WasEscalated { get; init; }

    /// <summary>Incident ticket ID if created</summary>
    public string? IncidentId { get; init; }

    /// <summary>Whether notification was sent</summary>
    public bool NotificationSent { get; init; }

    /// <summary>Users notified</summary>
    public List<Guid> NotifiedUserIds { get; init; } = [];

    /// <summary>Error message if escalation failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Timestamp of escalation</summary>
    public DateTime ProcessedAt { get; init; } = SystemClock.UtcNow;

    public static SlaEscalationResult Success(string? incidentId, List<Guid>? notifiedUsers = null) => new()
    {
        WasEscalated = true,
        IncidentId = incidentId,
        NotificationSent = true,
        NotifiedUserIds = notifiedUsers ?? []
    };

    public static SlaEscalationResult NotRequired() => new()
    {
        WasEscalated = false,
        NotificationSent = false
    };

    public static SlaEscalationResult Failed(string error) => new()
    {
        WasEscalated = false,
        NotificationSent = false,
        ErrorMessage = error
    };
}

/// <summary>
///     Configuration for SLA escalation behavior per tenant.
/// </summary>
public record SlaEscalationConfig
{
    /// <summary>Tenant ID</summary>
    public Guid TenantId { get; init; }

    /// <summary>Whether auto-escalation is enabled</summary>
    public bool AutoEscalationEnabled { get; init; } = true;

    /// <summary>Minimum severity to trigger auto-escalation</summary>
    public SlaViolationSeverity MinimumEscalationSeverity { get; init; } = SlaViolationSeverity.High;

    /// <summary>Email addresses to notify on escalation</summary>
    public List<string> EscalationEmails { get; init; } = [];

    /// <summary>User IDs to notify on escalation</summary>
    public List<Guid> EscalationUserIds { get; init; } = [];

    /// <summary>Whether to create incident tickets automatically</summary>
    public bool AutoCreateIncidents { get; init; } = true;

    /// <summary>External ticketing system URL (e.g., Jira, ServiceNow)</summary>
    public string? ExternalTicketingUrl { get; init; }

    /// <summary>Webhook URL for external integrations</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Cooldown period between notifications for same violation type (minutes)</summary>
    public int NotificationCooldownMinutes { get; init; } = 15;
}
