namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for managing user sessions and device tracking
/// </summary>
public interface ISessionManagementService
{
    Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, string? deviceFingerprint = null);

    Task<UserSession?> GetSessionAsync(Guid sessionId);

    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken);

    Task<List<UserSession>> GetUserSessionsAsync(Guid userId, bool activeOnly = true);

    Task<bool> ValidateSessionAsync(Guid sessionId);

    Task<bool> RefreshSessionAsync(Guid sessionId);

    Task<bool> TerminateSessionAsync(Guid sessionId, SessionTerminationReason reason);

    Task<int> TerminateAllUserSessionsAsync(Guid userId, SessionTerminationReason reason, Guid? exceptSessionId = null);

    Task<bool> TrustDeviceAsync(Guid userId, string deviceFingerprint, string deviceName);

    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint);

    Task<List<TrustedDevice>> GetTrustedDevicesAsync(Guid userId);

    Task<bool> RevokeTrustedDeviceAsync(Guid userId, Guid deviceId);

    Task CleanupExpiredSessionsAsync();

    Task<SessionSecurityAnalysis> AnalyzeSessionSecurityAsync(Guid userId, string ipAddress, string userAgent);
}
