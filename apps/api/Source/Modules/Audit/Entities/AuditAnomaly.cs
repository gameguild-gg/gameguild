using GameGuild.Core.Domain;

namespace GameGuild.Modules.Audit.Entities;

/// <summary>
/// Real-time anomaly detection on privileged operations for security monitoring.
/// Uses pattern analysis, ML-based detection, and rule-based triggers to identify suspicious activities.
/// </summary>
public sealed class AuditAnomaly : EntityBase
{
    public override Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public AnomalyType Type { get; private set; }
    public AnomalySeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime DetectedAt { get; private set; }

    // Detection details
    public string DetectionMethod { get; private set; } = string.Empty;
    public double ConfidenceScore { get; private set; }
    public string? DetectionRule { get; private set; }
    public string? PatternMatched { get; private set; }
    public string AnomalyData { get; private set; } = string.Empty;

    // Geographic context
    public string IpAddress { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public string? Region { get; private set; }
    public string? City { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public bool IsSuspiciousLocation { get; private set; }
    public double? DistanceFromLastLogin { get; private set; }

    // Related audit entries
    public string? RelatedAuditLogIds { get; private set; }
    public int RelatedEventCount { get; private set; }
    public DateTime? FirstRelatedEventAt { get; private set; }
    public DateTime? LastRelatedEventAt { get; private set; }

    // Response tracking
    public AnomalyStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? InvestigatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public string? MitigationActions { get; private set; }

    // Notification tracking
    public bool NotificationSent { get; private set; }
    public DateTime? NotificationSentAt { get; private set; }
    public string? NotificationChannel { get; private set; }

    private AuditAnomaly() { }

    public static AuditAnomaly Create(
        Guid tenantId,
        Guid? userId,
        AnomalyType type,
        AnomalySeverity severity,
        string title,
        string description,
        string detectionMethod,
        double confidenceScore,
        string ipAddress,
        string anomalyData)
    {
        return new AuditAnomaly
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Severity = severity,
            Title = title,
            Description = description,
            DetectionMethod = detectionMethod,
            ConfidenceScore = confidenceScore,
            IpAddress = ipAddress,
            AnomalyData = anomalyData,
            DetectedAt = DateTime.UtcNow,
            Status = AnomalyStatus.Detected,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetGeographicContext(string? country, string? region, string? city, double? latitude, double? longitude, bool isSuspicious, double? distance)
    {
        Country = country;
        Region = region;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
        IsSuspiciousLocation = isSuspicious;
        DistanceFromLastLogin = distance;
    }

    public void SetDetectionDetails(string? rule, string? pattern)
    {
        DetectionRule = rule;
        PatternMatched = pattern;
    }

    public void SetRelatedEvents(string auditLogIds, int eventCount, DateTime firstEventAt, DateTime lastEventAt)
    {
        RelatedAuditLogIds = auditLogIds;
        RelatedEventCount = eventCount;
        FirstRelatedEventAt = firstEventAt;
        LastRelatedEventAt = lastEventAt;
    }

    public void AssignTo(string assignee)
    {
        AssignedTo = assignee;
        AssignedAt = DateTime.UtcNow;
        Status = AnomalyStatus.Assigned;
    }

    public void MarkAsInvestigating()
    {
        InvestigatedAt = DateTime.UtcNow;
        Status = AnomalyStatus.Investigating;
    }

    public void Resolve(string resolutionNotes, string? mitigationActions = null)
    {
        ResolutionNotes = resolutionNotes;
        MitigationActions = mitigationActions;
        ResolvedAt = DateTime.UtcNow;
        Status = AnomalyStatus.Resolved;
    }

    public void MarkAsFalsePositive(string notes)
    {
        ResolutionNotes = notes;
        ResolvedAt = DateTime.UtcNow;
        Status = AnomalyStatus.FalsePositive;
    }

    public void MarkNotificationSent(string channel)
    {
        NotificationSent = true;
        NotificationSentAt = DateTime.UtcNow;
        NotificationChannel = channel;
    }
}

public enum AnomalyType
{
    UnusualAccessPattern,
    PrivilegedOperationSpike,
    GeographicAnomaly,
    TimeBasedAnomaly,
    MassDataExport,
    UnauthorizedAccess,
    SuspiciousPermissionChange,
    MultipleFailedAttempts,
    DataExfiltration,
    AccountCompromise,
    InsiderThreat,
    Other
}

public enum AnomalySeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum AnomalyStatus
{
    Detected,
    Assigned,
    Investigating,
    Resolved,
    FalsePositive,
    Dismissed
}
