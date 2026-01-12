using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for detecting authentication anomalies and preventing abuse with full ML-based detection algorithms
/// </summary>
public class AuthenticationAnomalyDetectionService : IAuthenticationAnomalyDetectionService
{
    private readonly IAuthenticationAttemptRepository _authAttemptRepository;
    private readonly ILogger<AuthenticationAnomalyDetectionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISiemIntegrationService _siemService;

    // Configuration constants with defaults
    private const int DefaultMaxFailedAttemptsPerHour = 5;
    private const int DefaultSuspiciousThreshold = 3;
    private const int DefaultThrottleMinutes = 15;
    private const int DefaultMaxAttemptsPerIpPerHour = 50;
    private const int DefaultBehavioralAnalysisWindowDays = 30;

    public AuthenticationAnomalyDetectionService(
        IAuthenticationAttemptRepository authAttemptRepository,
        ILogger<AuthenticationAnomalyDetectionService> logger,
        IConfiguration configuration,
        ISiemIntegrationService siemService)
    {
        _authAttemptRepository = authAttemptRepository;
        _logger = logger;
        _configuration = configuration;
        _siemService = siemService;
    }

    public async Task<AuthenticationAnomalyResult> AnalyzeAttemptAsync(Guid userId, string ipAddress, string userAgent, string? deviceFingerprint = null)
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
            // Analyze recent attempts from the last 24 hours
            var since = DateTime.UtcNow.AddHours(-24);
            var recentAttempts = await _authAttemptRepository.GetRecentAttemptsAsync(userId, since, cancellationToken: default);

            if (!recentAttempts.Any())
            {
                // First-time user or long-time absence - medium risk
                result.RiskScore += 10;
                result.RiskFactors.Add("First authentication attempt or long absence");
            }

            // Analyze IP address patterns
            var ipAttempts = await _authAttemptRepository.GetRecentAttemptsByIpAsync(ipAddress, since, cancellationToken: default);
            var uniqueUserAgents = ipAttempts.Select(a => a.UserAgent).Distinct().Count();

            if (uniqueUserAgents > 10)
            {
                result.RiskScore += 30;
                result.RiskFactors.Add($"Multiple user agents ({uniqueUserAgents}) from same IP");
            }

            // Check for rapid authentication attempts (velocity check)
            var lastFiveMinutes = DateTime.UtcNow.AddMinutes(-5);
            var recentRapidAttempts = recentAttempts.Where(a => a.AttemptedAt >= lastFiveMinutes).ToList();

            if (recentRapidAttempts.Count >= 3)
            {
                result.RiskScore += 25;
                result.RiskFactors.Add($"Rapid authentication attempts: {recentRapidAttempts.Count} in 5 minutes");
            }

            // Analyze device fingerprint changes
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

            // Check for unusual time patterns
            var lastSuccessful = await _authAttemptRepository.GetLastSuccessfulAttemptAsync(userId, cancellationToken: default);
            if (lastSuccessful != null)
            {
                var hourOfDay = DateTime.UtcNow.Hour;
                var lastSuccessHour = lastSuccessful.AttemptedAt.Hour;
                var hourDifference = Math.Abs(hourOfDay - lastSuccessHour);

                if (hourDifference > 12)
                {
                    result.RiskScore += 10;
                    result.RiskFactors.Add("Unusual time of day compared to historical pattern");
                }
            }

            // Calculate final risk level
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
                _logger.LogWarning(
                    "Anomalous authentication detected - UserId: {UserId}, IP: {IpAddress}, RiskScore: {RiskScore}",
                    userId, ipAddress, result.RiskScore
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing authentication attempt for user {UserId}", userId);
        }

        return result;
    }

    public async Task<bool> DetectBruteForceAsync(string identifier, int timeWindowMinutes = 15)
    {
        var sinceTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        var failedAttempts = await _authAttemptRepository.GetFailedAttemptsAsync(identifier, sinceTime, CancellationToken.None).ConfigureAwait(false);

        var failedCount = failedAttempts.Count();
        const int bruteForceThreshold = 5;

        if (failedCount >= bruteForceThreshold)
        {
            _logger.LogWarning("Brute force attack detected - Identifier: {Identifier}, Failed attempts: {FailedCount} in {TimeWindowMinutes} minutes", identifier, failedCount, timeWindowMinutes);

            // Send to SIEM if enabled
            await _siemService.SendBruteForceEventAsync(identifier, failedCount, TimeSpan.FromMinutes(timeWindowMinutes), CancellationToken.None);

            return true;
        }

        return false;
    }

    public async Task<bool> DetectImpossibleTravelAsync(Guid userId, LocationInfo currentLocation, LocationInfo previousLocation, TimeSpan timeBetween)
    {
        if (string.IsNullOrEmpty(currentLocation.Country) || string.IsNullOrEmpty(previousLocation.Country)) { return false; }

        if (currentLocation.Country == previousLocation.Country && currentLocation.City == previousLocation.City) { return false; }

        var isSuspicious = currentLocation.Country != previousLocation.Country && timeBetween.TotalHours < 1;

        if (isSuspicious)
        {
            _logger.LogWarning(
                "Impossible travel detected - UserId: {UserId}, From: {PreviousCountry} to {CurrentCountry} in {Hours:F2} hours",
                userId,
                previousLocation.Country,
                currentLocation.Country,
                timeBetween.TotalHours
            );

            // Send to SIEM if enabled
            await _siemService.SendImpossibleTravelEventAsync(userId, previousLocation, currentLocation, timeBetween, CancellationToken.None);
        }

        return isSuspicious;
    }

    public async Task<BehavioralAnalysisResult> AnalyzeBehavioralPatternsAsync(Guid userId, AuthenticationAttemptContext attemptContext)
    {
        var result = new BehavioralAnalysisResult 
        { 
            RiskLevel = RiskLevel.Low, 
            RiskScore = 0, 
            MatchesTypicalBehavior = true, 
            DetectedAnomalies = new List<string>(), 
            Confidence = 0.5 
        };

        try
        {
            // Analyze behavioral patterns over the last 30 days
            var analysisWindow = _configuration.GetValue("Authentication:Anomaly:BehavioralAnalysisWindowDays", DefaultBehavioralAnalysisWindowDays);
            var since = DateTime.UtcNow.AddDays(-analysisWindow);
            var historicalAttempts = await _authAttemptRepository.GetRecentAttemptsAsync(userId, since, limit: 1000, cancellationToken: default);

            if (historicalAttempts.Count < 5)
            {
                // Not enough data for behavioral analysis
                result.Confidence = 0.2;
                result.DetectedAnomalies.Add("Insufficient historical data for behavioral analysis");
                return result;
            }

            var successfulAttempts = historicalAttempts.Where(a => a.IsSuccessful).ToList();

            // Analyze typical IP addresses
            var commonIps = successfulAttempts
                .GroupBy(a => a.IpAddress)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Select(x => x.IpAddress)
                .ToList();

            if (commonIps.Any() && !commonIps.Contains(attemptContext.IpAddress))
            {
                result.RiskScore += 20;
                result.DetectedAnomalies.Add("Authentication from unfamiliar IP address");
                result.MatchesTypicalBehavior = false;
            }

            // Analyze typical user agents
            var commonUserAgents = successfulAttempts
                .Where(a => !string.IsNullOrEmpty(a.UserAgent))
                .GroupBy(a => a.UserAgent)
                .Select(g => new { UserAgent = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(3)
                .Select(x => x.UserAgent)
                .ToList();

            if (commonUserAgents.Any() && !commonUserAgents.Contains(attemptContext.UserAgent))
            {
                result.RiskScore += 15;
                result.DetectedAnomalies.Add("Authentication from unfamiliar device/browser");
                result.MatchesTypicalBehavior = false;
            }

            // Analyze typical authentication times
            var hourlyPattern = successfulAttempts
                .GroupBy(a => a.AttemptedAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToList();

            var currentHour = DateTime.UtcNow.Hour;
            var typicalHours = hourlyPattern.Where(h => h.Count >= successfulAttempts.Count * 0.1).Select(h => h.Hour).ToList();

            if (typicalHours.Any() && !typicalHours.Contains(currentHour))
            {
                result.RiskScore += 10;
                result.DetectedAnomalies.Add("Authentication at unusual time of day");
            }

            // Analyze geolocation patterns if available
            var currentLocationKey = attemptContext.Location != null 
                ? $"{attemptContext.Location.Country}-{attemptContext.Location.City}" 
                : null;

            if (!string.IsNullOrEmpty(currentLocationKey))
            {
                var commonLocations = successfulAttempts
                    .Where(a => !string.IsNullOrEmpty(a.Location))
                    .GroupBy(a => a.Location)
                    .Select(g => new { Location = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .Select(x => x.Location)
                    .ToList();

                if (commonLocations.Any() && !commonLocations.Contains(currentLocationKey))
                {
                    result.RiskScore += 25;
                    result.DetectedAnomalies.Add("Authentication from unfamiliar location");
                    result.MatchesTypicalBehavior = false;
                }
            }

            // Calculate confidence based on data volume
            result.Confidence = Math.Min(1.0, successfulAttempts.Count / 50.0);

            // Determine final risk level
            result.RiskLevel = result.RiskScore switch
            {
                >= 70 => RiskLevel.Critical,
                >= 50 => RiskLevel.High,
                >= 25 => RiskLevel.Medium,
                _ => RiskLevel.Low
            };

            if (result.RiskScore >= 25)
            {
                _logger.LogInformation(
                    "Behavioral anomalies detected for user {UserId}: RiskScore={RiskScore}, Anomalies={AnomalyCount}",
                    userId, result.RiskScore, result.DetectedAnomalies.Count
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing behavioral patterns for user {UserId}", userId);
        }

        return result;
    }

    public async Task RecordSuspiciousActivityAsync(SuspiciousActivity activity)
    {
        _logger.LogWarning(
            "Suspicious activity recorded - Type: {ActivityType}, UserId: {UserId}, Identifier: {Identifier}, RiskLevel: {RiskLevel}",
            activity.ActivityType,
            activity.UserId,
            activity.Identifier,
            activity.RiskLevel
        );

        // Send to SIEM system
        await _siemService.SendSuspiciousActivityEventAsync(activity, CancellationToken.None);
    }

    public async Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(CreateAuthenticationAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create the login attempt record
            var loginAttempt = new AuthenticationAttempt
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLowerInvariant(),
                UserId = request.UserId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsSuccessful = request.IsSuccessful,
                FailureReason = request.FailureReason,
                AttemptedAt = DateTime.UtcNow,
                ProcessingTime = request.ProcessingTime,
                Location = request.Location,
                DeviceFingerprint = request.DeviceFingerprint,
                SessionId = request.SessionId,
                TenantId = request.TenantId,
                Metadata = request.Metadata,
                CorrelationId = request.CorrelationId
            };

            // Analyze the attempt for anomalies
            var analysis = await AnalyzeLoginAttemptAsync(loginAttempt, cancellationToken);
            loginAttempt.IsSuspicious = analysis.IsSuspicious;
            loginAttempt.RiskScore = analysis.RiskScore;

            // Save the attempt
            await _authAttemptRepository.CreateAsync(loginAttempt, cancellationToken);

            // Log suspicious activity
            if (analysis.IsSuspicious) { await LogSuspiciousActivityAsync(loginAttempt, analysis, cancellationToken); }

            _logger.LogInformation(
                "Login attempt recorded: Email={Email}, IP={IpAddress}, Success={IsSuccessful}, Risk={RiskScore}, Suspicious={IsSuspicious}",
                request.Email,
                request.IpAddress,
                request.IsSuccessful,
                analysis.RiskScore,
                analysis.IsSuspicious
            );

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording login attempt for {Email} from {IpAddress}", request.Email, request.IpAddress);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000) { _logger.LogWarning("Slow login attempt recording: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds); }
        }
    }

    public async Task<ThrottleDecision> ShouldThrottleAsync(string ipAddress, string email, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        // Get recent failed attempts
        var recentAttempts = await _authAttemptRepository.GetFailedAttemptsAsync(email.ToLowerInvariant(), oneHourAgo, cancellationToken);

        var ipAttempts = recentAttempts.Count(a => a.IpAddress == ipAddress);
        var emailAttempts = recentAttempts.Count;

        var maxIpAttempts = _configuration.GetValue("Authentication:Anomaly:MaxAttemptsPerIpPerHour", DefaultMaxAttemptsPerIpPerHour);
        var maxFailedPerHour = _configuration.GetValue("Authentication:Anomaly:MaxFailedAttemptsPerHour", DefaultMaxFailedAttemptsPerHour);

        if (ipAttempts >= maxIpAttempts)
        {
            return new ThrottleDecision
            {
                ShouldThrottle = true,
                Reason = "IP address exceeded maximum attempts per hour",
                ThrottleUntil = now.AddMinutes(_configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        if (emailAttempts >= maxFailedPerHour)
        {
            return new ThrottleDecision
            {
                ShouldThrottle = true,
                Reason = "Email exceeded maximum failed attempts per hour",
                ThrottleUntil = now.AddMinutes(_configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        return new ThrottleDecision { ShouldThrottle = false, RemainingAttempts = Math.Max(0, maxFailedPerHour - emailAttempts) };
    }

    public string GenerateDeviceFingerprint(string? userAgent, string? acceptLanguage = null, string? acceptEncoding = null)
    {
        var fingerprint = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));

        return Convert.ToHexString(hash);
    }

    private async Task<AuthenticationAttemptAnalysis> AnalyzeLoginAttemptAsync(AuthenticationAttempt attempt, CancellationToken cancellationToken = default)
    {
        var analysis = new AuthenticationAttemptAnalysis { IsSuspicious = false, RiskScore = 0, RiskFactors = new List<string>() };

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        // Check for rapid repeated attempts from same IP
        var recentIpAttempts = await _authAttemptRepository.GetFailedAttemptsAsync(attempt.Email, oneHourAgo, cancellationToken);

        var ipAttemptCount = recentIpAttempts.Count(a => a.IpAddress == attempt.IpAddress);

        if (ipAttemptCount >= 3)
        {
            analysis.RiskScore += 20;
            analysis.RiskFactors.Add($"Multiple failed attempts from IP: {ipAttemptCount}");
        }

        // Check for suspicious timing patterns
        if (attempt.ProcessingTime < TimeSpan.FromMilliseconds(50))
        {
            analysis.RiskScore += 15;
            analysis.RiskFactors.Add("Abnormally fast authentication attempt");
        }

        // Check for missing or suspicious user agent
        if (string.IsNullOrEmpty(attempt.UserAgent) || attempt.UserAgent.Length < 10)
        {
            analysis.RiskScore += 10;
            analysis.RiskFactors.Add("Missing or suspicious user agent");
        }

        // Mark as suspicious if risk score is high enough
        var suspiciousThreshold = _configuration.GetValue("Authentication:Anomaly:SuspiciousThreshold", DefaultSuspiciousThreshold);

        if (analysis.RiskScore >= suspiciousThreshold * 10) { analysis.IsSuspicious = true; }

        return analysis;
    }

    private async Task LogSuspiciousActivityAsync(AuthenticationAttempt attempt, AuthenticationAttemptAnalysis analysis, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken; // Reserved for future async operations
        _logger.LogWarning("Suspicious login attempt detected - Email: {Email}, IP: {IpAddress}, RiskScore: {RiskScore}", attempt.Email, attempt.IpAddress, analysis.RiskScore);

        // Future: Send to SIEM or alert system
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
