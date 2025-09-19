using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Service interface for authentication anomaly detection
/// </summary>
public interface IAuthenticationAnomalyService {
    /// <summary>
    /// Records a login attempt and analyzes it for suspicious patterns
    /// </summary>
    Task<LoginAttemptAnalysis> RecordLoginAttemptAsync(CreateLoginAttemptRequest request);

    /// <summary>
    /// Checks if an IP address should be throttled due to suspicious activity
    /// </summary>
    Task<ThrottleDecision> ShouldThrottleAsync(string ipAddress, string email);

    /// <summary>
    /// Generates a device fingerprint from user agent and other headers
    /// </summary>
    string GenerateDeviceFingerprint(string? userAgent, string? acceptLanguage = null, string? acceptEncoding = null);

    /// <summary>
    /// Analyzes login patterns for a user to detect anomalies
    /// </summary>
    Task<UserLoginAnalysis> AnalyzeUserLoginPatternsAsync(Guid userId, string currentIpAddress, string? currentUserAgent);

    /// <summary>
    /// Gets recent suspicious activity for monitoring
    /// </summary>
    Task<IEnumerable<SuspiciousActivity>> GetRecentSuspiciousActivityAsync(TimeSpan? timeWindow = null);
}

/// <summary>
/// Service for detecting authentication anomalies and preventing abuse
/// </summary>
public class AuthenticationAnomalyService : IAuthenticationAnomalyService {
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthenticationAnomalyService> _logger;
    private readonly IConfiguration _configuration;

    // Configuration constants with defaults
    private const int DefaultMaxFailedAttemptsPerHour = 5;
    private const int DefaultMaxFailedAttemptsPerDay = 20;
    private const int DefaultSuspiciousThreshold = 3;
    private const int DefaultThrottleMinutes = 15;
    private const int DefaultMaxAttemptsPerIpPerHour = 50;

    public AuthenticationAnomalyService(
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<AuthenticationAnomalyService> logger,
        IConfiguration configuration) {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LoginAttemptAnalysis> RecordLoginAttemptAsync(CreateLoginAttemptRequest request) {
        var stopwatch = Stopwatch.StartNew();

        try {
            // Create the login attempt record
            var loginAttempt = new LoginAttempt {
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
            _context.LoginAttempts.Add(loginAttempt);
            await _context.SaveChangesAsync();

            // Log suspicious activity
            if (analysis.IsSuspicious) {
                await LogSuspiciousActivityAsync(loginAttempt, analysis);
            }

            _logger.LogInformation(
                "Login attempt recorded: Email={Email}, IP={IpAddress}, Success={IsSuccessful}, Risk={RiskScore}, Suspicious={IsSuspicious}",
                request.Email, request.IpAddress, request.IsSuccessful, analysis.RiskScore, analysis.IsSuspicious);

            return analysis;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error recording login attempt for {Email} from {IpAddress}", request.Email, request.IpAddress);
            throw;
        }
        finally {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000) {
                _logger.LogWarning("Slow login attempt recording: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }
    }

    public async Task<ThrottleDecision> ShouldThrottleAsync(string ipAddress, string email) {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);
        var oneDayAgo = now.AddDays(-1);

        // Check IP-based throttling
        var ipAttempts = await _context.LoginAttempts
            .Where(la => la.IpAddress == ipAddress && la.AttemptedAt >= oneHourAgo)
            .CountAsync();

        var maxIpAttempts = _configuration.GetValue("Authentication:Anomaly:MaxAttemptsPerIpPerHour", DefaultMaxAttemptsPerIpPerHour);

        if (ipAttempts >= maxIpAttempts) {
            return new ThrottleDecision {
                ShouldThrottle = true,
                Reason = "IP address exceeded maximum attempts per hour",
                ThrottleUntil = now.AddMinutes(_configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        // Check email-based throttling for failed attempts
        var emailFailedAttempts = await _context.LoginAttempts
            .Where(la => la.Email == email.ToLowerInvariant() &&
                        !la.IsSuccessful &&
                        la.AttemptedAt >= oneHourAgo)
            .CountAsync();

        var maxFailedPerHour = _configuration.GetValue("Authentication:Anomaly:MaxFailedAttemptsPerHour", DefaultMaxFailedAttemptsPerHour);

        if (emailFailedAttempts >= maxFailedPerHour) {
            return new ThrottleDecision {
                ShouldThrottle = true,
                Reason = "Email exceeded maximum failed attempts per hour",
                ThrottleUntil = now.AddMinutes(_configuration.GetValue("Authentication:Anomaly:ThrottleMinutes", DefaultThrottleMinutes)),
                RemainingAttempts = 0
            };
        }

        // Check daily limits
        var dailyFailedAttempts = await _context.LoginAttempts
            .Where(la => la.Email == email.ToLowerInvariant() &&
                        !la.IsSuccessful &&
                        la.AttemptedAt >= oneDayAgo)
            .CountAsync();

        var maxFailedPerDay = _configuration.GetValue("Authentication:Anomaly:MaxFailedAttemptsPerDay", DefaultMaxFailedAttemptsPerDay);

        if (dailyFailedAttempts >= maxFailedPerDay) {
            return new ThrottleDecision {
                ShouldThrottle = true,
                Reason = "Email exceeded maximum failed attempts per day",
                ThrottleUntil = now.AddHours(1), // Longer throttle for daily limit
                RemainingAttempts = 0
            };
        }

        return new ThrottleDecision {
            ShouldThrottle = false,
            RemainingAttempts = Math.Max(0, maxFailedPerHour - emailFailedAttempts)
        };
    }

    public string GenerateDeviceFingerprint(string? userAgent, string? acceptLanguage = null, string? acceptEncoding = null) {
        var fingerprint = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<UserLoginAnalysis> AnalyzeUserLoginPatternsAsync(Guid userId, string currentIpAddress, string? currentUserAgent) {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var recentAttempts = await _context.LoginAttempts
            .Where(la => la.UserId == userId && la.AttemptedAt >= thirtyDaysAgo && la.IsSuccessful)
            .OrderByDescending(la => la.AttemptedAt)
            .Take(50)
            .ToListAsync();

        var analysis = new UserLoginAnalysis {
            UserId = userId,
            IsNewLocation = false,
            IsNewDevice = false,
            IsUnusualTime = false,
            RecentSuccessfulLogins = recentAttempts.Count,
            UniqueLo
        };

        if (!recentAttempts.Any()) {
            analysis.IsNewUser = true;
            return analysis;
        }

        // Check for new IP address
        var knownIps = recentAttempts.Select(ra => ra.IpAddress).Distinct().ToList();
        analysis.IsNewLocation = !knownIps.Contains(currentIpAddress);

        // Check for new device
        if (!string.IsNullOrEmpty(currentUserAgent)) {
            var currentFingerprint = GenerateDeviceFingerprint(currentUserAgent);
            var knownFingerprints = recentAttempts
                .Where(ra => !string.IsNullOrEmpty(ra.DeviceFingerprint))
                .Select(ra => ra.DeviceFingerprint!)
                .Distinct()
                .ToList();

            analysis.IsNewDevice = !knownFingerprints.Contains(currentFingerprint);
        }

        // Check for unusual time patterns
        var currentHour = DateTime.UtcNow.Hour;
        var typicalHours = recentAttempts
            .Select(ra => ra.AttemptedAt.Hour)
            .GroupBy(h => h)
            .OrderByDescending(g => g.Count())
            .Take(6) // Top 6 hours
            .Select(g => g.Key)
            .ToList();

        analysis.IsUnusualTime = !typicalHours.Contains(currentHour);

        // Calculate additional metrics
        analysis.UniqueLocations = knownIps.Count;
        analysis.UniqueDevices = recentAttempts
            .Where(ra => !string.IsNullOrEmpty(ra.DeviceFingerprint))
            .Select(ra => ra.DeviceFingerprint!)
            .Distinct()
            .Count();

        return analysis;
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetRecentSuspiciousActivityAsync(TimeSpan? timeWindow = null) {
        var cutoff = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromHours(24));

        var suspiciousAttempts = await _context.LoginAttempts
            .Where(la => la.IsSuspicious && la.AttemptedAt >= cutoff)
            .GroupBy(la => new { la.IpAddress, la.Email })
            .Select(g => new SuspiciousActivity {
                IpAddress = g.Key.IpAddress,
                Email = g.Key.Email,
                AttemptCount = g.Count(),
                FirstAttempt = g.Min(la => la.AttemptedAt),
                LastAttempt = g.Max(la => la.AttemptedAt),
                MaxRiskScore = g.Max(la => la.RiskScore),
                UniqueUserAgents = g.Select(la => la.UserAgent).Distinct().Count(),
                SuccessfulAttempts = g.Count(la => la.IsSuccessful)
            })
            .OrderByDescending(sa => sa.MaxRiskScore)
            .ToListAsync();

        return suspiciousAttempts;
    }

    private async Task<LoginAttemptAnalysis> AnalyzeLoginAttemptAsync(LoginAttempt attempt) {
        var riskScore = 0;
        var riskFactors = new List<string>();

        // Analyze recent failed attempts from same IP
        var recentFailedFromIp = await _context.LoginAttempts
            .Where(la => la.IpAddress == attempt.IpAddress &&
                        !la.IsSuccessful &&
                        la.AttemptedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        if (recentFailedFromIp >= 3) {
            riskScore += 30;
            riskFactors.Add($"Recent failed attempts from IP: {recentFailedFromIp}");
        }

        // Analyze recent failed attempts for same email
        var recentFailedForEmail = await _context.LoginAttempts
            .Where(la => la.Email == attempt.Email &&
                        !la.IsSuccessful &&
                        la.AttemptedAt >= DateTime.UtcNow.AddMinutes(-15))
            .CountAsync();

        if (recentFailedForEmail >= 2) {
            riskScore += 25;
            riskFactors.Add($"Recent failed attempts for email: {recentFailedForEmail}");
        }

        // Check for user existence patterns (potential enumeration)
        if (!attempt.IsSuccessful && attempt.FailureReason == LoginFailureReasons.InvalidCredentials) {
            var recentEnumerationAttempts = await _context.LoginAttempts
                .Where(la => la.IpAddress == attempt.IpAddress &&
                            la.FailureReason == LoginFailureReasons.InvalidCredentials &&
                            la.AttemptedAt >= DateTime.UtcNow.AddMinutes(-30))
                .CountAsync();

            if (recentEnumerationAttempts >= 5) {
                riskScore += 40;
                riskFactors.Add($"Potential user enumeration: {recentEnumerationAttempts} attempts");
            }
        }

        // Analyze timing patterns (too fast could indicate automation)
        if (attempt.ProcessingTime < TimeSpan.FromMilliseconds(100)) {
            riskScore += 15;
            riskFactors.Add("Suspiciously fast authentication attempt");
        }

        // Check for unusual user agent patterns
        if (string.IsNullOrEmpty(attempt.UserAgent) || attempt.UserAgent.Length < 20) {
            riskScore += 10;
            riskFactors.Add("Missing or suspicious user agent");
        }

        // Geographic analysis (if location is available)
        if (attempt.UserId.HasValue && !string.IsNullOrEmpty(attempt.Location)) {
            var userAnalysis = await AnalyzeUserLoginPatternsAsync(attempt.UserId.Value, attempt.IpAddress, attempt.UserAgent);
            if (userAnalysis.IsNewLocation) {
                riskScore += 20;
                riskFactors.Add("Login from new geographic location");
            }
            if (userAnalysis.IsNewDevice) {
                riskScore += 15;
                riskFactors.Add("Login from new device");
            }
        }

        var isSuspicious = riskScore >= _configuration.GetValue("Authentication:Anomaly:SuspiciousThreshold", DefaultSuspiciousThreshold * 10); // Scale up for score

        return new LoginAttemptAnalysis {
            RiskScore = Math.Min(riskScore, 100), // Cap at 100
            IsSuspicious = isSuspicious,
            RiskFactors = riskFactors,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private async Task LogSuspiciousActivityAsync(LoginAttempt attempt, LoginAttemptAnalysis analysis) {
        try {
            await _auditService.LogAsync(new CreateAuditLogRequest {
                ActionType = AuditActionTypes.SuspiciousActivity,
                ResourceType = "Authentication",
                ResourceId = attempt.Id.ToString(),
                UserId = attempt.UserId,
                TenantId = attempt.TenantId,
                IpAddress = attempt.IpAddress,
                UserAgent = attempt.UserAgent,
                SessionId = attempt.SessionId,
                Description = $"Suspicious login attempt detected for {attempt.Email}",
                Metadata = new {
                    RiskScore = analysis.RiskScore,
                    RiskFactors = analysis.RiskFactors,
                    Email = attempt.Email,
                    FailureReason = attempt.FailureReason
                },
                Success = false,
                RiskLevel = analysis.RiskScore >= 70 ? AuditRiskLevel.High : AuditRiskLevel.Medium,
                Category = AuditCategory.Security,
                CorrelationId = attempt.CorrelationId
            });

            _logger.LogWarning(
                "Suspicious login attempt: Email={Email}, IP={IpAddress}, RiskScore={RiskScore}, Factors={@RiskFactors}",
                attempt.Email, attempt.IpAddress, analysis.RiskScore, analysis.RiskFactors);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to log suspicious activity for login attempt {AttemptId}", attempt.Id);
        }
    }
}

/// <summary>
/// Analysis result for a login attempt
/// </summary>
public class LoginAttemptAnalysis {
    public int RiskScore { get; set; }
    public bool IsSuspicious { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Decision about whether to throttle an authentication attempt
/// </summary>
public class ThrottleDecision {
    public bool ShouldThrottle { get; set; }
    public string? Reason { get; set; }
    public DateTime? ThrottleUntil { get; set; }
    public int RemainingAttempts { get; set; }
}

/// <summary>
/// Analysis of user login patterns
/// </summary>
public class UserLoginAnalysis {
    public Guid UserId { get; set; }
    public bool IsNewUser { get; set; }
    public bool IsNewLocation { get; set; }
    public bool IsNewDevice { get; set; }
    public bool IsUnusualTime { get; set; }
    public int RecentSuccessfulLogins { get; set; }
    public int UniqueLocations { get; set; }
    public int UniqueDevices { get; set; }
}

/// <summary>
/// Suspicious activity summary for monitoring
/// </summary>
public class SuspiciousActivity {
    public string IpAddress { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime FirstAttempt { get; set; }
    public DateTime LastAttempt { get; set; }
    public int MaxRiskScore { get; set; }
    public int UniqueUserAgents { get; set; }
    public int SuccessfulAttempts { get; set; }
}