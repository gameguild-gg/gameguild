namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service interface for authentication anomaly detection
/// </summary>
public interface IAuthenticationAnomalyDetectionService
{
    /// <summary>
    /// Records a login attempt and analyzes it for suspicious patterns
    /// </summary>
    Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(CreateAuthenticationAttemptRequest request);

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
    Task<UserSignInAnalysis> AnalyzeUserLoginPatternsAsync(Guid userId, string currentIpAddress, string? currentUserAgent);

    /// <summary>
    /// Gets recent suspicious activity for monitoring
    /// </summary>
    Task<IEnumerable<SuspiciousActivity>> GetRecentSuspiciousActivityAsync(TimeSpan? timeWindow = null);
}
