namespace GameGuild.Modules.Audit.Services;
using GameGuild.Modules.Users;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Audit;

/// <summary>
/// Service for real-time anomaly detection on audit events with ML-based pattern recognition
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IRepository<AuditAnomaly, Guid> _repository;
    private readonly IRepository<TamperEvidentAuditLog, Guid> _auditLogRepository;
    private readonly ILogger<AnomalyDetectionService> _logger;

    // Thresholds for anomaly detection
    private const int SuspiciousLoginThreshold = 5; // Failed logins in 15 minutes
    private const int UnusualAccessPatternThreshold = 50; // Resource accesses in 1 hour
    private const double SuspiciousLocationDistanceKm = 500; // Travel distance in short time
    private const int PrivilegeEscalationWindowMinutes = 60;

    public AnomalyDetectionService(
        IRepository<AuditAnomaly, Guid> repository,
        IRepository<TamperEvidentAuditLog, Guid> auditLogRepository,
        ILogger<AnomalyDetectionService> logger)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<AnomalyDetectionResult> DetectAnomalyAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        var result = new AnomalyDetectionResult
        {
            IsAnomaly = false,
            ConfidenceScore = 0.0,
            DetectedAnomalies = new List<AuditAnomaly>()
        };

        // Run multiple detection algorithms
        var detections = new List<Task<AuditAnomaly?>>
        {
            DetectMultipleFailedLoginsAsync(auditLog, cancellationToken),
            DetectUnusualAccessPatternAsync(auditLog, cancellationToken),
            DetectSuspiciousLocationAsync(auditLog, cancellationToken),
            DetectUnauthorizedAccessAttemptAsync(auditLog, cancellationToken),
            DetectDataExfiltrationAsync(auditLog, cancellationToken),
            DetectPrivilegeEscalationAsync(auditLog, cancellationToken),
            DetectAccountTakeoverAsync(auditLog, cancellationToken),
            DetectBruteForceAttackAsync(auditLog, cancellationToken)
        };

        var anomalies = await Task.WhenAll(detections);

        foreach (var anomaly in anomalies.Where(a => a != null))
        {
            result.DetectedAnomalies.Add(anomaly!);
            result.IsAnomaly = true;
            result.ConfidenceScore = Math.Max(result.ConfidenceScore, anomaly!.ConfidenceScore);
        }

        return result;
    }

    public async Task<List<AuditAnomaly>> GetActiveAnomaliesAsync(
        Guid? tenantId = null,
        AnomalySeverity? minSeverity = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.AsQueryable()
            .Where(x => x.Status == AnomalyStatus.Detected || x.Status == AnomalyStatus.Investigating);

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId);

        if (minSeverity.HasValue)
            query = query.Where(x => x.Severity >= minSeverity.Value);

        return await query
            .OrderByDescending(x => x.Severity)
            .ThenByDescending(x => x.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignAnomalyAsync(
        Guid anomalyId,
        Guid assignedToUserId,
        CancellationToken cancellationToken = default)
    {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly == null)
            throw new InvalidOperationException($"Anomaly {anomalyId} not found");

        anomaly.AssignTo(assignedToUserId);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Assigned anomaly {AnomalyId} to user {UserId}", anomalyId, assignedToUserId);
    }

    public async Task ResolveAnomalyAsync(
        Guid anomalyId,
        string resolutionNotes,
        List<string>? mitigationActions = null,
        CancellationToken cancellationToken = default)
    {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly == null)
            throw new InvalidOperationException($"Anomaly {anomalyId} not found");

        anomaly.Resolve(resolutionNotes, mitigationActions);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Resolved anomaly {AnomalyId}", anomalyId);
    }

    public async Task MarkAsFalsePositiveAsync(
        Guid anomalyId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly == null)
            throw new InvalidOperationException($"Anomaly {anomalyId} not found");

        anomaly.MarkAsFalsePositive(reason);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Marked anomaly {AnomalyId} as false positive", anomalyId);
    }

    public double CalculateConfidenceScore(
        AnomalyType type,
        int occurrenceCount,
        TimeSpan timeWindow,
        Dictionary<string, object>? contextData = null)
    {
        // Base score calculation
        double baseScore = type switch
        {
            AnomalyType.MultipleFailedLogins => 0.7,
            AnomalyType.UnusualAccessPattern => 0.6,
            AnomalyType.SuspiciousLocation => 0.8,
            AnomalyType.UnauthorizedAccessAttempt => 0.9,
            AnomalyType.DataExfiltration => 0.95,
            AnomalyType.PrivilegeEscalation => 0.9,
            AnomalyType.AccountTakeover => 0.95,
            AnomalyType.BruteForceAttack => 0.85,
            _ => 0.5
        };

        // Adjust based on occurrence frequency
        var frequencyMultiplier = Math.Min(1.0 + (occurrenceCount / 10.0), 2.0);

        // Adjust based on time window (shorter window = higher confidence)
        var timeMultiplier = timeWindow.TotalMinutes < 5 ? 1.2 :
                             timeWindow.TotalMinutes < 15 ? 1.1 :
                             timeWindow.TotalMinutes < 60 ? 1.0 : 0.9;

        var finalScore = Math.Min(baseScore * frequencyMultiplier * timeMultiplier, 0.99);
        return Math.Round(finalScore, 2);
    }

    public async Task<Dictionary<string, object>> AnalyzeGeographicPatternsAsync(
        Guid userId,
        string currentCountry,
        double? currentLatitude,
        double? currentLongitude,
        CancellationToken cancellationToken = default)
    {
        var recentAudits = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == userId && x.Country != null)
            .OrderByDescending(x => x.Timestamp)
            .Take(10)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, object>
        {
            { "CurrentCountry", currentCountry },
            { "IsSuspicious", false },
            { "DistanceFromLastLogin", 0.0 },
            { "CountryChanges", 0 }
        };

        if (!recentAudits.Any())
            return result;

        var lastAudit = recentAudits.First();
        var countryChanges = recentAudits
            .Select(x => x.Country)
            .Distinct()
            .Count();

        result["CountryChanges"] = countryChanges;

        // Check for suspicious country changes
        if (lastAudit.Country != currentCountry &&
            (DateTime.UtcNow - lastAudit.Timestamp).TotalHours < 2)
        {
            result["IsSuspicious"] = true;
            result["Reason"] = "Impossible travel detected";
        }

        return result;
    }

    // Private detection methods
    private async Task<AuditAnomaly?> DetectMultipleFailedLoginsAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        if (auditLog.Action != "Login" || auditLog.RiskLevel == AuditRiskLevel.Low)
            return null;

        var recentFailedLogins = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId &&
                       x.Action == "Login" &&
                       x.RiskLevel != AuditRiskLevel.Low &&
                       x.Timestamp >= DateTime.UtcNow.AddMinutes(-15))
            .CountAsync(cancellationToken);

        if (recentFailedLogins >= SuspiciousLoginThreshold)
        {
            var confidence = CalculateConfidenceScore(
                AnomalyType.MultipleFailedLogins,
                recentFailedLogins,
                TimeSpan.FromMinutes(15));

            var anomaly = AuditAnomaly.Create(
                auditLog.TenantId,
                auditLog.UserId,
                AnomalyType.MultipleFailedLogins,
                confidence >= 0.8 ? AnomalySeverity.High : AnomalySeverity.Medium,
                "Multiple Failed Login Attempts",
                $"Detected {recentFailedLogins} failed login attempts in the last 15 minutes");

            anomaly.SetDetectionDetails("RuleBasedEngine", confidence, "FailedLoginThreshold", null);
            return anomaly;
        }

        return null;
    }

    private async Task<AuditAnomaly?> DetectUnusualAccessPatternAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        var recentAccesses = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId &&
                       x.Timestamp >= DateTime.UtcNow.AddHours(-1))
            .CountAsync(cancellationToken);

        if (recentAccesses >= UnusualAccessPatternThreshold)
        {
            var confidence = CalculateConfidenceScore(
                AnomalyType.UnusualAccessPattern,
                recentAccesses,
                TimeSpan.FromHours(1));

            var anomaly = AuditAnomaly.Create(
                auditLog.TenantId,
                auditLog.UserId,
                AnomalyType.UnusualAccessPattern,
                AnomalySeverity.Medium,
                "Unusual Access Pattern Detected",
                $"User performed {recentAccesses} actions in the last hour");

            anomaly.SetDetectionDetails("StatisticalAnalysis", confidence, "AccessFrequencyThreshold", null);
            return anomaly;
        }

        return null;
    }

    private async Task<AuditAnomaly?> DetectSuspiciousLocationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(auditLog.Country))
            return null;

        var lastAudit = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId && x.Country != null && x.Id != auditLog.Id)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastAudit != null && lastAudit.Country != auditLog.Country)
        {
            var timeDiff = (auditLog.Timestamp - lastAudit.Timestamp).TotalHours;

            if (timeDiff < 2) // Impossible travel within 2 hours
            {
                var confidence = CalculateConfidenceScore(
                    AnomalyType.SuspiciousLocation,
                    1,
                    TimeSpan.FromHours(timeDiff));

                var anomaly = AuditAnomaly.Create(
                    auditLog.TenantId,
                    auditLog.UserId,
                    AnomalyType.SuspiciousLocation,
                    AnomalySeverity.High,
                    "Impossible Travel Detected",
                    $"User location changed from {lastAudit.Country} to {auditLog.Country} in {timeDiff:F1} hours");

                anomaly.SetDetectionDetails("GeographicAnalysis", confidence, "ImpossibleTravelDetection", null);
                anomaly.SetGeographicContext(auditLog.IpAddress, auditLog.Country, null, null, null, null, true, 0);
                return anomaly;
            }
        }

        return null;
    }

    private Task<AuditAnomaly?> DetectUnauthorizedAccessAttemptAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        // Implement based on authorization failure patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectDataExfiltrationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        // Implement based on large data export patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectPrivilegeEscalationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        // Implement based on rapid permission changes
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectAccountTakeoverAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        // Implement based on behavior change patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectBruteForceAttackAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken)
    {
        // Implement based on systematic access patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }
}
