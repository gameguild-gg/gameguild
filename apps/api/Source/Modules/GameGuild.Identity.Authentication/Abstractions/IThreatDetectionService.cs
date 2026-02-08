namespace GameGuild.Identity.Authentication;

/// <summary>
///     Detects brute-force attacks, impossible-travel scenarios, and applies throttling logic.
/// </summary>
public interface IThreatDetectionService
{
    /// <summary>
    ///     Detects if multiple failed authentication attempts indicate a brute force attack.
    /// </summary>
    Task<bool> DetectBruteForceAsync(string identifier, int timeWindowMinutes = 15);

    /// <summary>
    ///     Checks for impossible travel scenarios (login from distant locations in short time).
    /// </summary>
    Task<bool> DetectImpossibleTravelAsync(
        Guid userId,
        LocationInfo currentLocation,
        LocationInfo previousLocation,
        TimeSpan timeBetween);

    /// <summary>
    ///     Determines whether a login attempt should be throttled based on recent failure rates.
    /// </summary>
    Task<ThrottleDecision> ShouldThrottleAsync(
        string ipAddress,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a SHA-256 device fingerprint from browser metadata.
    /// </summary>
    string GenerateDeviceFingerprint(
        string? userAgent,
        string? acceptLanguage = null,
        string? acceptEncoding = null);
}
