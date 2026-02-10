using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Thin facade that preserves the original <see cref="IAuthenticationAnomalyDetectionService" /> contract
///     while delegating to focused sub-services.
/// </summary>
public class AuthenticationAnomalyDetectionService(
    IAuthenticationAttemptRepository authAttemptRepository,
    IThreatDetectionService threatDetectionService,
    IBehavioralAnalysisService behavioralAnalysisService,
    ILoginAttemptAnalysisService loginAttemptAnalysisService,
    ILogger<AuthenticationAnomalyDetectionService> logger) : IAuthenticationAnomalyDetectionService
{
    // ── IAuthenticationAnomalyDetectionService — anomaly analysis ─────

    public async Task<AuthenticationAnomalyResult> AnalyzeAttemptAsync(
        Guid userId,
        string ipAddress,
        string userAgent,
        string? deviceFingerprint = null)
    {
        var result = new AuthenticationAnomalyResult
        {
            IsAnomalous = false,
            RiskLevel = RiskLevel.Low,
            RiskScore = 0,
            RiskFactors = new List<string>()
        };

        try
        {
            var since = SystemClock.UtcNow.AddHours(-24);
            var recentAttempts = await authAttemptRepository
                .GetRecentAttemptsAsync(userId, since, cancellationToken: default)
                .ConfigureAwait(false);

            if (!recentAttempts.Any())
            {
                result.RiskScore += 10;
                result.RiskFactors.Add("First authentication attempt or long absence");
            }

            var ipAttempts = await authAttemptRepository
                .GetRecentAttemptsByIpAsync(ipAddress, since, cancellationToken: default)
                .ConfigureAwait(false);
            var uniqueUserAgents = ipAttempts.Select(a => a.UserAgent).Distinct().Count();

            if (uniqueUserAgents > 10)
            {
                result.RiskScore += 30;
                result.RiskFactors.Add($"Multiple user agents ({uniqueUserAgents}) from same IP");
            }

            var lastFiveMinutes = SystemClock.UtcNow.AddMinutes(-5);
            var recentRapidAttempts = recentAttempts.Where(a => a.AttemptedAt >= lastFiveMinutes).ToList();

            if (recentRapidAttempts.Count >= 3)
            {
                result.RiskScore += 25;
                result.RiskFactors.Add($"Rapid authentication attempts: {recentRapidAttempts.Count} in 5 minutes");
            }

            if (!string.IsNullOrEmpty(deviceFingerprint))
            {
                var knownFingerprints = recentAttempts
                    .Where(a => !string.IsNullOrEmpty(a.DeviceFingerprint))
                    .Select(a => a.DeviceFingerprint)
                    .Distinct()
                    .ToList();

                if (knownFingerprints.Any() && !knownFingerprints.Contains(deviceFingerprint))
                {
                    result.RiskScore += 15;
                    result.RiskFactors.Add("New device fingerprint");
                }
            }

            var lastSuccessful = await authAttemptRepository
                .GetLastSuccessfulAttemptAsync(userId, cancellationToken: default)
                .ConfigureAwait(false);

            if (lastSuccessful != null)
            {
                var hourOfDay = SystemClock.UtcNow.Hour;
                var lastSuccessHour = lastSuccessful.AttemptedAt.Hour;
                var hourDifference = Math.Abs(hourOfDay - lastSuccessHour);

                if (hourDifference > 12)
                {
                    result.RiskScore += 10;
                    result.RiskFactors.Add("Unusual time of day compared to historical pattern");
                }
            }

            result.RiskLevel = result.RiskScore switch
            {
                >= 80 => RiskLevel.Critical,
                >= 60 => RiskLevel.High,
                >= 30 => RiskLevel.Medium,
                _ => RiskLevel.Low
            };

            result.IsAnomalous = result.RiskScore >= 30;

            if (result.IsAnomalous)
            {
                logger.LogWarning(
                    "Anomalous authentication detected - UserId: {UserId}, IP: {IpAddress}, RiskScore: {RiskScore}",
                    userId, ipAddress, result.RiskScore);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing authentication attempt for user {UserId}", userId);
            throw;
        }

        return result;
    }

    // ── Delegated to IThreatDetectionService ──────────────────────────

    public Task<bool> DetectBruteForceAsync(string identifier, int timeWindowMinutes = 15)
        => threatDetectionService.DetectBruteForceAsync(identifier, timeWindowMinutes);

    public Task<bool> DetectImpossibleTravelAsync(
        Guid userId,
        LocationInfo currentLocation,
        LocationInfo previousLocation,
        TimeSpan timeBetween)
        => threatDetectionService.DetectImpossibleTravelAsync(userId, currentLocation, previousLocation, timeBetween);

    // ── Delegated to IBehavioralAnalysisService ──────────────────────

    public Task<BehavioralAnalysisResult> AnalyzeBehavioralPatternsAsync(
        Guid userId,
        AuthenticationAttemptContext attemptContext)
        => behavioralAnalysisService.AnalyzeBehavioralPatternsAsync(userId, attemptContext);

    public Task RecordSuspiciousActivityAsync(SuspiciousActivity activity)
        => loginAttemptAnalysisService.RecordSuspiciousActivityAsync(activity);

    public Task<AuthenticationAnomalyResult> AnalyzeLoginAttemptAsync(AuthenticationAttemptContext context)
        => loginAttemptAnalysisService.AnalyzeLoginAttemptAsync(context);

    // ── Extra public methods (used directly by callers of the concrete class) ──

    public Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(
        CreateAuthenticationAttemptRequest request,
        CancellationToken cancellationToken = default)
        => loginAttemptAnalysisService.RecordLoginAttemptAsync(request, cancellationToken);

    public Task<ThrottleDecision> ShouldThrottleAsync(
        string ipAddress,
        string email,
        CancellationToken cancellationToken = default)
        => threatDetectionService.ShouldThrottleAsync(ipAddress, email, cancellationToken);

    public string GenerateDeviceFingerprint(
        string? userAgent,
        string? acceptLanguage = null,
        string? acceptEncoding = null)
        => threatDetectionService.GenerateDeviceFingerprint(userAgent, acceptLanguage, acceptEncoding);
}
