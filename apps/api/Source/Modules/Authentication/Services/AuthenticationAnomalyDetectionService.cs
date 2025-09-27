using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Modules.Audit;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for detecting authentication anomalies and preventing abuse
/// </summary>
public class AuthenticationAnomalyDetectionService(IAuthenticationAttemptRepository authAttemptRepository, IAuditService auditService, ILogger<AuthenticationAnomalyDetectionService> logger, IConfiguration configuration)
    : IAuthenticationAnomalyDetectionService
{
    private readonly IAuditService _auditService = auditService;

    // Configuration constants with defaults
    private const int DefaultMaxFailedAttemptsPerHour = 5;

    private const int DefaultMaxFailedAttemptsPerDay = 20;

    private const int DefaultSuspiciousThreshold = 3;

    private const int DefaultThrottleMinutes = 15;

    private const int DefaultMaxAttemptsPerIpPerHour = 50;

    public async Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(CreateAuthenticationAttemptRequest request)
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
                CorrelationId = request.CorrelationId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Analyze the attempt for anomalies
            var analysis = await AnalyzeLoginAttemptAsync(loginAttempt);
            loginAttempt.IsSuspicious = analysis.IsSuspicious;
            loginAttempt.RiskScore = analysis.RiskScore;

            // Save the attempt
            await authAttemptRepository.CreateAsync(loginAttempt);

            // Log suspicious activity
            if (analysis.IsSuspicious) { await LogSuspiciousActivityAsync(loginAttempt, analysis); }

            logger.LogInformation(
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
            logger.LogError(ex, "Error recording login attempt for {Email} from {IpAddress}", request.Email, request.IpAddress);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000) { logger.LogWarning("Slow login attempt recording: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds); }
        }
    }

    public async Task<ThrottleDecision> ShouldThrottleAsync(string ipAddress, string email)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var oneDayAgo = now.AddDays(-1);

        // Check IP-based throttling
        var ipAttempts = await authAttemptRepository.CountFailedAttemptsByIpAsync(ipAddress, oneHourAgo);

        var maxIpAttempts = configuration.GetValue("Authentication:Anomaly:MaxAttemptsPerIpPerHour", DefaultMaxAttemptsPerIpPerHour);

        if (ipAttempts >= maxIpAttempts)
        {
            return new ThrottleDecision
            {
                ShouldThrottle = true,
                Reason = "IP address exceeded maximum attempts per hour",
                ThrottleUntil = now.AddMinutes(configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        // Check email-based throttling for failed attempts
        var emailFailedAttempts = await authAttemptRepository.CountFailedAttemptsAsync(email.ToLowerInvariant(), oneHourAgo);

        var maxFailedPerHour = configuration.GetValue("Authentication:Anomaly:MaxFailedAttemptsPerHour", DefaultMaxFailedAttemptsPerHour);

        if (emailFailedAttempts >= maxFailedPerHour)
        {
            return new ThrottleDecision
            {
                ShouldThrottle = true,
                Reason = "Email exceeded maximum failed attempts per hour",
                ThrottleUntil = now.AddMinutes(configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        // Check daily limits
        var dailyFailedAttempts = await authAttemptRepository.CountFailedAttemptsAsync(email.ToLowerInvariant(), oneDayAgo);

        var maxFailedPerDay = configuration.GetValue("Authentication:Anomaly:MaxFailedAttemptsPerDay", DefaultMaxFailedAttemptsPerDay);

        if (dailyFailedAttempts >= maxFailedPerDay)
        {
            return new ThrottleDecision
            {
                ShouldThrottle = true,
                Reason = "Email exceeded maximum failed attempts per day",
                ThrottleUntil = now.AddHours(1), // Longer throttle for daily limit
                RemainingAttempts = 0
            };
        }

        return new ThrottleDecision { ShouldThrottle = false, RemainingAttempts = Math.Max(0, maxFailedPerHour - emailFailedAttempts) };
    }

    public string GenerateDeviceFingerprint(string? userAgent, string? acceptLanguage = null, string? acceptEncoding = null)
    {
        var fingerprint = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<UserSignInAnalysis> AnalyzeUserLoginPatternsAsync(Guid userId, string currentIpAddress, string? currentUserAgent)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var recentAttempts = await authAttemptRepository.GetByUserIdAsync(userId, 50);

        var analysis = new UserSignInAnalysis { UserId = userId, IsNewLocation = false, IsNewDevice = false, IsUnusualTime = false, RecentSuccessfulLogins = recentAttempts.Count, UniqueLocations = 0, UniqueDevices = 0 };

        if (!recentAttempts.Any())
        {
            analysis.IsNewUser = true;

            return analysis;
        }

        // Check for new IP address
        var knownIps = recentAttempts.Select(ra => ra.IpAddress).Distinct().ToList();
        analysis.IsNewLocation = !knownIps.Contains(currentIpAddress);

        // Check for new device
        if (!string.IsNullOrEmpty(currentUserAgent))
        {
            var currentFingerprint = GenerateDeviceFingerprint(currentUserAgent);
            var knownFingerprints = recentAttempts.Where(ra => !string.IsNullOrEmpty(ra.DeviceFingerprint)).Select(ra => ra.DeviceFingerprint!).Distinct().ToList();

            analysis.IsNewDevice = !knownFingerprints.Contains(currentFingerprint);
        }

        // Check for unusual time patterns
        var currentHour = DateTime.UtcNow.Hour;

        var typicalHours = recentAttempts.Select(ra => ra.AttemptedAt.Hour)
            .GroupBy(h => h)
            .OrderByDescending(g => g.Count())
            .Take(6) // Top 6 hours
            .Select(g => g.Key)
            .ToList();

        analysis.IsUnusualTime = !typicalHours.Contains(currentHour);

        // Calculate additional metrics
        analysis.UniqueLocations = knownIps.Count;
        analysis.UniqueDevices = recentAttempts.Where(ra => !string.IsNullOrEmpty(ra.DeviceFingerprint)).Select(ra => ra.DeviceFingerprint!).Distinct().Count();

        return analysis;
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetRecentSuspiciousActivityAsync(TimeSpan? timeWindow = null)
    {
        var cutoff = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromHours(24));

        var suspiciousAttempts = await authAttemptRepository.GetSuspiciousAttemptsAsync(cutoff);

        // Group and analyze suspicious attempts in memory
        var groupedSuspicious = suspiciousAttempts
            .GroupBy(la => new { la.IpAddress, la.Email })
            .Select(g => new SuspiciousActivity
            {
                IpAddress = g.Key.IpAddress,
                Email = g.Key.Email,
                AttemptCount = g.Count(),
                FirstAttempt = g.Min(la => la.AttemptedAt),
                LastAttempt = g.Max(la => la.AttemptedAt),
                MaxRiskScore = g.Max(la => la.RiskScore),
                UniqueUserAgents = g.Select(la => la.UserAgent).Distinct().Count(),
                SuccessfulAttempts = g.Count(la => la.IsSuccessful)
            }
            )
            .OrderByDescending(sa => sa.MaxRiskScore)
            .ToList();

        return groupedSuspicious;
    }

    private async Task<AuthenticationAttemptAnalysis> AnalyzeLoginAttemptAsync(AuthenticationAttempt attempt)
    {
        var riskScore = 0;
        var riskFactors = new List<string>();

        // Analyze recent failed attempts from same IP
        var recentFailedFromIp = await authAttemptRepository.CountFailedAttemptsByIpAsync(attempt.IpAddress, DateTime.UtcNow.AddHours(-1));

        if (recentFailedFromIp >= 3)
        {
            riskScore += 30;
            riskFactors.Add($"Recent failed attempts from IP: {recentFailedFromIp}");
        }

        // Analyze recent failed attempts for same email
        var recentFailedForEmail = await authAttemptRepository.CountFailedAttemptsAsync(attempt.Email, DateTime.UtcNow.AddMinutes(-15));

        if (recentFailedForEmail >= 2)
        {
            riskScore += 25;
            riskFactors.Add($"Recent failed attempts for email: {recentFailedForEmail}");
        }

        // Check for user existence patterns (potential enumeration)
        if (attempt is { IsSuccessful: false, FailureReason: AuthenticationFailureReasons.InvalidCredentials })
        {
            var recentIpAttempts = await authAttemptRepository.GetByIpAddressAsync(attempt.IpAddress, DateTime.UtcNow.AddMinutes(-30));
            var recentEnumerationAttempts = recentIpAttempts.Count(la => la.FailureReason == AuthenticationFailureReasons.InvalidCredentials);

            if (recentEnumerationAttempts >= 5)
            {
                riskScore += 40;
                riskFactors.Add($"Potential user enumeration: {recentEnumerationAttempts} attempts");
            }
        }

        // Analyze timing patterns (too fast could indicate automation)
        if (attempt.ProcessingTime < TimeSpan.FromMilliseconds(100))
        {
            riskScore += 15;
            riskFactors.Add("Suspiciously fast authentication attempt");
        }

        // Check for unusual user agent patterns
        if (string.IsNullOrEmpty(attempt.UserAgent) || attempt.UserAgent.Length < 20)
        {
            riskScore += 10;
            riskFactors.Add("Missing or suspicious user agent");
        }

        // Geographic analysis (if location is available)
        if (attempt.UserId.HasValue && !string.IsNullOrEmpty(attempt.Location))
        {
            var userAnalysis = await AnalyzeUserLoginPatternsAsync(attempt.UserId.Value, attempt.IpAddress, attempt.UserAgent);

            if (userAnalysis.IsNewLocation)
            {
                riskScore += 20;
                riskFactors.Add("Login from new geographic location");
            }

            if (userAnalysis.IsNewDevice)
            {
                riskScore += 15;
                riskFactors.Add("Login from new device");
            }
        }

        var isSuspicious = riskScore >= configuration.GetValue("Authentication:Anomaly:SuspiciousThreshold", DefaultSuspiciousThreshold * 10); // Scale up for score

        return new AuthenticationAttemptAnalysis
        {
            RiskScore = Math.Min(riskScore, 100), // Cap at 100
            IsSuspicious = isSuspicious,
            RiskFactors = riskFactors,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private async Task LogSuspiciousActivityAsync(AuthenticationAttempt attempt, AuthenticationAttemptAnalysis analysis)
    {
        try
        {
            await _auditService.LogAsync(
                new CreateAuditLogRequest
                {
                    ActionType = AuditActionTypes.SuspiciousActivity,
                    ResourceType = "Authentication",
                    ResourceId = attempt.Id.ToString(),
                    UserId = attempt.UserId,
                    TenantId = attempt.TenantId,
                    IpAddress = attempt.IpAddress,
                    UserAgent = attempt.UserAgent,
                    SessionId = attempt.SessionId,
                    Description = $"Suspicious login attempt detected for {attempt.Email}",
                    Metadata = new { RiskScore = analysis.RiskScore, RiskFactors = analysis.RiskFactors, Email = attempt.Email, FailureReason = attempt.FailureReason },
                    Success = false,
                    RiskLevel = analysis.RiskScore >= 70 ? AuditRiskLevel.High : AuditRiskLevel.Medium,
                    Category = AuditCategory.Security,
                    CorrelationId = attempt.CorrelationId
                }
            );

            logger.LogWarning("Suspicious login attempt: Email={Email}, IP={IpAddress}, RiskScore={RiskScore}, Factors={@RiskFactors}", attempt.Email, attempt.IpAddress, analysis.RiskScore, analysis.RiskFactors);
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to log suspicious activity for login attempt {AttemptId}", attempt.Id); }
    }
}
