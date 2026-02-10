using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records login attempts, runs inline risk analysis, persists results, and
///     composes higher-level anomaly results from attempt context.
/// </summary>
public class LoginAttemptAnalysisService(
    IAuthenticationAttemptRepository authAttemptRepository,
    IThreatDetectionService threatDetectionService,
    ILogger<LoginAttemptAnalysisService> logger,
    IConfiguration configuration,
    ISiemIntegrationService siemService) : ILoginAttemptAnalysisService
{
    private const int DefaultSuspiciousThreshold = 3;

    public async Task RecordSuspiciousActivityAsync(SuspiciousActivity activity)
    {
        logger.LogWarning(
            "Suspicious activity recorded - Type: {ActivityType}, UserId: {UserId}, Identifier: {Identifier}, RiskLevel: {RiskLevel}",
            activity.ActivityType,
            activity.UserId,
            activity.Identifier,
            activity.RiskLevel);

        await siemService
            .SendSuspiciousActivityEventAsync(activity, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public async Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(
        CreateAuthenticationAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var loginAttempt = new AuthenticationAttempt
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLowerInvariant(),
                UserId = request.UserId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsSuccessful = request.IsSuccessful,
                FailureReason = request.FailureReason,
                AttemptedAt = SystemClock.UtcNow,
                ProcessingTime = request.ProcessingTime,
                Location = request.Location,
                DeviceFingerprint = request.DeviceFingerprint,
                SessionId = request.SessionId,
                TenantId = request.TenantId,
                Metadata = request.Metadata,
                CorrelationId = request.CorrelationId
            };

            var analysis = await AnalyzeLoginAttemptInternalAsync(loginAttempt, cancellationToken)
                .ConfigureAwait(false);
            loginAttempt.IsSuspicious = analysis.IsSuspicious;
            loginAttempt.RiskScore = analysis.RiskScore;

            await authAttemptRepository.CreateAsync(loginAttempt, cancellationToken).ConfigureAwait(false);

            if (analysis.IsSuspicious)
            {
                await LogSuspiciousActivityAsync(loginAttempt, analysis).ConfigureAwait(false);
            }

            logger.LogInformation(
                "Login attempt recorded: Email={Email}, IP={IpAddress}, Success={IsSuccessful}, Risk={RiskScore}, Suspicious={IsSuspicious}",
                request.Email,
                request.IpAddress,
                request.IsSuccessful,
                analysis.RiskScore,
                analysis.IsSuspicious);

            return analysis;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording login attempt for {Email} from {IpAddress}",
                request.Email, request.IpAddress);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                logger.LogWarning("Slow login attempt recording: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<AuthenticationAnomalyResult> AnalyzeLoginAttemptAsync(AuthenticationAttemptContext context)
    {
        var result = new AuthenticationAnomalyResult
        {
            IsAnomalous = false,
            RiskLevel = RiskLevel.Low,
            RiskScore = 0,
            DetectedAnomalies = new List<string>()
        };

        try
        {
            if (context.UserId.HasValue)
            {
                var userId = context.UserId.Value;
                var since = SystemClock.UtcNow.AddHours(-24);
                var recentAttempts = await authAttemptRepository
                    .GetRecentAttemptsAsync(userId, since, cancellationToken: default)
                    .ConfigureAwait(false);

                if (!recentAttempts.Any())
                {
                    result.RiskScore += 10;
                    result.DetectedAnomalies.Add("FirstAttemptOrLongAbsence");
                }

                var lastSuccessful = await authAttemptRepository
                    .GetLastSuccessfulAttemptAsync(userId, cancellationToken: default)
                    .ConfigureAwait(false);

                if (lastSuccessful != null && lastSuccessful.IpAddress != context.IpAddress)
                {
                    result.RiskScore += 20;
                    result.DetectedAnomalies.Add("IpAddressChange");
                }

                if (lastSuccessful != null && !string.IsNullOrEmpty(lastSuccessful.UserAgent) &&
                    lastSuccessful.UserAgent != context.UserAgent)
                {
                    result.RiskScore += 15;
                    result.DetectedAnomalies.Add("UserAgentChange");
                }

                if (context.Location != null && lastSuccessful != null)
                {
                    var timeBetween = context.AttemptedAt - lastSuccessful.AttemptedAt;
                    var previousLocation = ParseLocation(lastSuccessful.Location);

                    if (previousLocation != null)
                    {
                        var isImpossibleTravel = await threatDetectionService
                            .DetectImpossibleTravelAsync(userId, context.Location, previousLocation, timeBetween)
                            .ConfigureAwait(false);

                        if (isImpossibleTravel)
                        {
                            result.RiskScore += 50;
                            result.DetectedAnomalies.Add("ImpossibleTravel");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(context.DeviceFingerprint) && lastSuccessful != null &&
                    !string.IsNullOrEmpty(lastSuccessful.DeviceFingerprint) &&
                    lastSuccessful.DeviceFingerprint != context.DeviceFingerprint)
                {
                    result.RiskScore += 15;
                    result.DetectedAnomalies.Add("DeviceFingerprintChange");
                }
            }

            if (!string.IsNullOrEmpty(context.Identifier))
            {
                var isBruteForce = await threatDetectionService
                    .DetectBruteForceAsync(context.Identifier)
                    .ConfigureAwait(false);

                if (isBruteForce)
                {
                    result.RiskScore += 40;
                    result.DetectedAnomalies.Add("BruteForceDetected");
                }
            }

            if (context.IsWeekend || context.TimeOfDay.Hours < 6 || context.TimeOfDay.Hours > 22)
            {
                result.RiskScore += 5;
                result.DetectedAnomalies.Add("UnusualTimeOfDay");
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
                    "Anomalous login attempt detected - UserId: {UserId}, IP: {IpAddress}, RiskScore: {RiskScore}, Anomalies: {Anomalies}",
                    context.UserId, context.IpAddress, result.RiskScore,
                    string.Join(", ", result.DetectedAnomalies));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing login attempt context");
            throw;
        }

        return result;
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task<AuthenticationAttemptAnalysis> AnalyzeLoginAttemptInternalAsync(
        AuthenticationAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        var analysis = new AuthenticationAttemptAnalysis
        {
            IsSuspicious = false,
            RiskScore = 0,
            RiskFactors = new List<string>()
        };

        var oneHourAgo = SystemClock.UtcNow.AddHours(-1);

        var recentIpAttempts = await authAttemptRepository
            .GetFailedAttemptsAsync(attempt.Email, oneHourAgo, cancellationToken)
            .ConfigureAwait(false);

        var ipAttemptCount = recentIpAttempts.Count(a => a.IpAddress == attempt.IpAddress);

        if (ipAttemptCount >= 3)
        {
            analysis.RiskScore += 20;
            analysis.RiskFactors.Add($"Multiple failed attempts from IP: {ipAttemptCount}");
        }

        if (attempt.ProcessingTime < TimeSpan.FromMilliseconds(50))
        {
            analysis.RiskScore += 15;
            analysis.RiskFactors.Add("Abnormally fast authentication attempt");
        }

        if (string.IsNullOrEmpty(attempt.UserAgent) || attempt.UserAgent.Length < 10)
        {
            analysis.RiskScore += 10;
            analysis.RiskFactors.Add("Missing or suspicious user agent");
        }

        var suspiciousThreshold = configuration.GetValue(
            "Authentication:Anomaly:SuspiciousThreshold", DefaultSuspiciousThreshold);

        if (analysis.RiskScore >= suspiciousThreshold * 10)
        {
            analysis.IsSuspicious = true;
        }

        return analysis;
    }

    private async Task LogSuspiciousActivityAsync(
        AuthenticationAttempt attempt,
        AuthenticationAttemptAnalysis analysis)
    {
        logger.LogWarning(
            "Suspicious login attempt detected - Email: {Email}, IP: {IpAddress}, RiskScore: {RiskScore}",
            attempt.Email, attempt.IpAddress, analysis.RiskScore);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static LocationInfo? ParseLocation(string? locationString)
    {
        if (string.IsNullOrEmpty(locationString))
        {
            return null;
        }

        var parts = locationString.Split('-');
        if (parts.Length >= 2)
        {
            return new LocationInfo { Country = parts[0], City = parts[1] };
        }

        return new LocationInfo { Country = locationString };
    }
}
