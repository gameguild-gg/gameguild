using GameGuild.Authentication.Models;
using GameGuild.Authentication.Models.Analysis;
using GameGuild.Authentication.Models.Flow;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Service for detecting anomalous authentication patterns and potential security threats.
///     Analyzes login attempts, device changes, location shifts, and behavioral patterns.
/// </summary>
public interface IAuthenticationAnomalyDetectionService
{
    /// <summary>
    ///     Analyzes an authentication attempt for suspicious patterns.
    ///     Checks for impossible travel, unusual login times, new devices, etc.
    /// </summary>
    /// <param name="userId">The user attempting to authenticate</param>
    /// <param name="ipAddress">The IP address of the attempt</param>
    /// <param name="userAgent">The user agent string</param>
    /// <param name="deviceFingerprint">Optional device fingerprint</param>
    /// <returns>Analysis result with risk score and detected anomalies</returns>
    Task<AuthenticationAnomalyResult> AnalyzeAttemptAsync(Guid userId, string ipAddress, string userAgent, string? deviceFingerprint = null);

    /// <summary>
    ///     Detects if multiple failed authentication attempts indicate a brute force attack.
    /// </summary>
    /// <param name="identifier">Email, username, or IP address to check</param>
    /// <param name="timeWindowMinutes">Time window to analyze (default: 15 minutes)</param>
    /// <returns>True if brute force attack is detected</returns>
    Task<bool> DetectBruteForceAsync(string identifier, int timeWindowMinutes = 15);

    /// <summary>
    ///     Checks for impossible travel scenarios (login from distant locations in short time).
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="currentLocation">Current login location</param>
    /// <param name="previousLocation">Previous login location</param>
    /// <param name="timeBetween">Time between the two logins</param>
    /// <returns>True if travel is physically impossible</returns>
    Task<bool> DetectImpossibleTravelAsync(Guid userId, LocationInfo currentLocation, LocationInfo previousLocation, TimeSpan timeBetween);

    /// <summary>
    ///     Analyzes if a login attempt matches the user's typical behavioral patterns.
    ///     Considers time of day, location, device type, and authentication frequency.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="attemptContext">Context of the current attempt</param>
    /// <returns>Behavioral analysis result with risk assessment</returns>
    Task<BehavioralAnalysisResult> AnalyzeBehavioralPatternsAsync(Guid userId, AuthenticationAttemptContext attemptContext);

    /// <summary>
    ///     Records a suspicious activity for future analysis and threat intelligence.
    /// </summary>
    /// <param name="activity">Details of the suspicious activity</param>
    Task RecordSuspiciousActivityAsync(SuspiciousActivity activity);
}
