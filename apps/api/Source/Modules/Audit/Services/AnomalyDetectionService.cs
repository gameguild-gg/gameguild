namespace GameGuild.Modules.Audit.Services;

using Users;
using Resources;
using Entities;
using Enums;
using CQRS;

/// <summary>
/// Service for real-time anomaly detection on audit events with ML-based pattern recognition
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService {
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
        ILogger<AnomalyDetectionService> logger) {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    // Interface method: DetectAnomalyAsync with specific parameters
    public async Task<Result<AuditAnomaly?>> DetectAnomalyAsync(
        Guid tenantId,
        Guid? userId,
        string action,
        string entityType,
        string ipAddress,
        string userAgent,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default) {
        // Create an audit log from the parameters to use with internal detection
        var auditLog = TamperEvidentAuditLog.Create(
            tenantId: tenantId,
            userId: userId,
            action: action,
            entityType: entityType,
            entityId: null,
            beforeSnapshot: null,
            afterSnapshot: null,
            changes: string.Empty,
            riskLevel: "Unknown",
            ipAddress: ipAddress,
            userAgent: userAgent,
            country: null,
            region: null,
            city: null,
            previousHash: string.Empty,
            sequenceNumber: 0
        );

        var result = await DetectAnomalyInternalAsync(auditLog, cancellationToken);
        var anomaly = result.DetectedAnomalies.FirstOrDefault();

        return Result<AuditAnomaly?>.Success(anomaly);
    }

    // Internal detection method
    private async Task<AnomalyDetectionResult> DetectAnomalyInternalAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken = default) {
        var result = new AnomalyDetectionResult {
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

        foreach (var anomaly in anomalies.Where(a => a != null)) {
            result.DetectedAnomalies.Add(anomaly!);
            result.IsAnomaly = true;
            result.ConfidenceScore = Math.Max(result.ConfidenceScore, anomaly!.ConfidenceScore);
        }

        return result;
    }

    public async Task<Result<IEnumerable<AuditAnomaly>>> GetActiveAnomaliesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) {
        var query = _repository.AsQueryable()
            .Where(x => x.TenantId == tenantId)
            .Where(x => x.Status == AnomalyStatus.Detected || x.Status == AnomalyStatus.Investigating);

        var anomalies = await query
            .OrderByDescending(x => x.Severity)
            .ThenByDescending(x => x.DetectedAt)
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<AuditAnomaly>>.Success(anomalies);
    }

    public async Task<Result> AssignAnomalyAsync(
        Guid anomalyId,
    string assignee,
    CancellationToken cancellationToken = default) {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly is null)
            return Result.Failure("Anomaly not found");

        anomaly.AssignTo(assignee);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Assigned anomaly {AnomalyId} to user {UserId}", anomalyId, assignee);
        return Result.Success();
    }
    public async Task<Result> ResolveAnomalyAsync(
            Guid anomalyId,
            string resolutionNotes,
            string? mitigationActions = null,
            CancellationToken cancellationToken = default) {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly is null)
            return Result.Failure("Anomaly not found");

        anomaly.Resolve(resolutionNotes, mitigationActions);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Resolved anomaly {AnomalyId}", anomalyId);
        return Result.Success();
    }

    public async Task<Result> MarkAsFalsePositiveAsync(
        Guid anomalyId,
        string notes,
        CancellationToken cancellationToken = default) {
        var anomaly = await _repository.GetByIdAsync(anomalyId, cancellationToken);
        if (anomaly is null)
            return Result.Failure("Anomaly not found");

        anomaly.MarkAsFalsePositive(notes);
        await _repository.UpdateAsync(anomaly, cancellationToken);

        _logger.LogInformation("Marked anomaly {AnomalyId} as false positive", anomalyId);
        return Result.Success();
    }

    public double CalculateConfidenceScore(Dictionary<string, object> features) {
        // Extract features from dictionary
        if (!features.TryGetValue("anomalyType", out var typeObj) ||
            !features.TryGetValue("occurrenceCount", out var countObj)) {
            return 0.5; // Default score if features are missing
        }

        var type = (AnomalyType)typeObj;
        var occurrenceCount = Convert.ToInt32(countObj);

        // Base score calculation
        double baseScore = type switch {
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
        double frequencyMultiplier = Math.Min(1.0 + (occurrenceCount / 10.0), 2.0);

        // Adjust based on time window if available
        double timeMultiplier = 1.0;
        if (features.TryGetValue("timeWindowMinutes", out var timeObj)) {
            var timeWindowMinutes = Convert.ToDouble(timeObj);
            timeMultiplier = timeWindowMinutes < 5 ? 1.2 :
                             timeWindowMinutes < 15 ? 1.1 :
                             timeWindowMinutes < 60 ? 1.0 : 0.9;
        }

        double finalScore = Math.Min(baseScore * frequencyMultiplier * timeMultiplier, 0.99);
        return Math.Round(finalScore, 2);
    }

    public async Task<Result<Dictionary<string, object>>> AnalyzeGeographicPatternsAsync(
        Guid userId,
        string ipAddress,
        CancellationToken cancellationToken = default) {
        var recentAudits = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == userId && x.Country != null)
            .OrderByDescending(x => x.Timestamp)
            .Take(10)
            .ToListAsync(cancellationToken);

        Dictionary<string, object> result = new Dictionary<string, object>
        {
            { "CurrentIpAddress", ipAddress },
            { "IsSuspicious", false },
            { "DistanceFromLastLogin", 0.0 },
            { "CountryChanges", 0 }
        };

        if (!recentAudits.Any())
            return Result<Dictionary<string, object>>.Success(result);

        var lastAudit = recentAudits.First();
        var countryChanges = recentAudits
            .Select(x => x.Country)
            .Distinct()
            .Count();

        result["CountryChanges"] = countryChanges;

        // Check for suspicious patterns
        if (countryChanges > 3 && (DateTime.UtcNow - recentAudits.Last().Timestamp).TotalHours < 24) {
            result["IsSuspicious"] = true;
            result["Reason"] = "Multiple country changes detected";
        }

        return Result<Dictionary<string, object>>.Success(result);
    }

    // Private detection methods
    private async Task<AuditAnomaly?> DetectMultipleFailedLoginsAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        if (auditLog.Action != "Login" || auditLog.RiskLevel == AuditRiskLevel.Low)
            return null;

        var recentFailedLogins = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId &&
                       x.Action == "Login" &&
                       x.RiskLevel != AuditRiskLevel.Low &&
                       x.Timestamp >= DateTime.UtcNow.AddMinutes(-15))
            .CountAsync(cancellationToken);

        if (recentFailedLogins >= SuspiciousLoginThreshold) {
            var confidence = CalculateConfidenceScore(new Dictionary<string, object> {
                { "anomalyType", AnomalyType.MultipleFailedLogins },
                { "occurrenceCount", recentFailedLogins },
                { "timeWindow", TimeSpan.FromMinutes(15) }
            });

            var anomaly = AuditAnomaly.Create(
              auditLog.TenantId,
              auditLog.UserId,
              AnomalyType.MultipleFailedLogins,
              confidence >= 0.8 ? AnomalySeverity.High : AnomalySeverity.Medium,
              "Multiple Failed Login Attempts",
              $"Detected {recentFailedLogins} failed login attempts in the last 15 minutes",
              "RuleBasedEngine",
              confidence,
              auditLog.IpAddress ?? string.Empty,
              System.Text.Json.JsonSerializer.Serialize(new { recentFailedLogins, threshold = SuspiciousLoginThreshold }));

            anomaly.SetDetectionDetails("RuleBasedEngine", confidence, "FailedLoginThreshold", null);
            return anomaly;
        }

        return null;
    }

    private async Task<AuditAnomaly?> DetectUnusualAccessPatternAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        var recentAccesses = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId &&
                       x.Timestamp >= DateTime.UtcNow.AddHours(-1))
            .CountAsync(cancellationToken);

        if (recentAccesses >= UnusualAccessPatternThreshold) {
            var confidence = CalculateConfidenceScore(new Dictionary<string, object> {
                { "anomalyType", AnomalyType.UnusualAccessPattern },
                { "occurrenceCount", recentAccesses },
                { "timeWindow", TimeSpan.FromHours(1) }
            });

            var anomaly = AuditAnomaly.Create(
              auditLog.TenantId,
              auditLog.UserId,
              AnomalyType.UnusualAccessPattern,
              AnomalySeverity.Medium,
              "Unusual Access Pattern Detected",
              $"User performed {recentAccesses} actions in the last hour",
              "StatisticalAnalysis",
              confidence,
              auditLog.IpAddress ?? string.Empty,
              System.Text.Json.JsonSerializer.Serialize(new { recentAccesses, threshold = UnusualAccessPatternThreshold }));

            anomaly.SetDetectionDetails("StatisticalAnalysis", confidence, "AccessFrequencyThreshold", null);
            return anomaly;
        }

        return null;
    }

    private async Task<AuditAnomaly?> DetectSuspiciousLocationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(auditLog.Country))
            return null;

        var lastAudit = await _auditLogRepository
            .AsQueryable()
            .Where(x => x.UserId == auditLog.UserId && x.Country != null && x.Id != auditLog.Id)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastAudit != null && lastAudit.Country != auditLog.Country) {
            var timeDiff = (auditLog.Timestamp - lastAudit.Timestamp).TotalHours;

            if (timeDiff < 2) // Impossible travel within 2 hours
            {
                var confidence = CalculateConfidenceScore(new Dictionary<string, object> {
                    { "anomalyType", AnomalyType.SuspiciousLocation },
                    { "occurrenceCount", 1 },
                    { "timeWindow", TimeSpan.FromHours(timeDiff) }
                });

                var anomaly = AuditAnomaly.Create(
                  auditLog.TenantId,
                  auditLog.UserId,
                  AnomalyType.SuspiciousLocation,
                  AnomalySeverity.High,
                  "Impossible Travel Detected",
                  $"User location changed from {lastAudit.Country} to {auditLog.Country} in {timeDiff:F1} hours",
                  "GeographicAnalysis",
                  confidence,
                  auditLog.IpAddress ?? string.Empty,
                  System.Text.Json.JsonSerializer.Serialize(new { fromCountry = lastAudit.Country, toCountry = auditLog.Country, timeDiffHours = timeDiff }));

                anomaly.SetDetectionDetails("GeographicAnalysis", confidence, "ImpossibleTravelDetection", null);
                anomaly.SetGeographicContext(auditLog.IpAddress, auditLog.Country, null, null, null, null, true, 0);
                return anomaly;
            }
        }

        return null;
    }

    private Task<AuditAnomaly?> DetectUnauthorizedAccessAttemptAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        // Implement based on authorization failure patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectDataExfiltrationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        // Implement based on large data export patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectPrivilegeEscalationAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        // Implement based on rapid permission changes
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectAccountTakeoverAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        // Implement based on behavior change patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }

    private Task<AuditAnomaly?> DetectBruteForceAttackAsync(
        TamperEvidentAuditLog auditLog,
        CancellationToken cancellationToken) {
        // Implement based on systematic access patterns
        return Task.FromResult<AuditAnomaly?>(null);
    }
}
