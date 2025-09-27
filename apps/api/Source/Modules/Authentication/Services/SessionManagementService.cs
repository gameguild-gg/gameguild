using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using Microsoft.Extensions.Options;
using UAParser;

namespace GameGuild.Modules.Authentication;

public class SessionManagementService(IUserSessionRepository userSessionRepository, ITrustedDeviceRepository trustedDeviceRepository, ILogger<SessionManagementService> logger, IOptions<SessionOptions> options) : ISessionManagementService
{
    private readonly SessionOptions _options = options.Value;

    public async Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, string? deviceFingerprint = null)
    {
        try
        {
            // Parse device information
            var deviceInfo = ParseDeviceInfo(userAgent);
            var location = await GetLocationFromIpAsync(ipAddress);

            // Generate device fingerprint if not provided
            deviceFingerprint ??= GenerateDeviceFingerprint(ipAddress, userAgent, deviceInfo);

            // Check if we need to enforce max sessions limit
            await EnforceMaxSessionsLimitAsync(userId);

            var session = new UserSession
            {
                UserId = userId,
                RefreshToken = GenerateRefreshToken(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                DeviceInfo = JsonSerializer.Serialize(deviceInfo),
                Location = location != null ? JsonSerializer.Serialize(location) : null,
                ExpiresAt = DateTime.UtcNow.Add(_options.SessionLifetime),
                LastUsedAt = DateTime.UtcNow,
                IsActive = true,
                IsTrustedDevice = await IsDeviceTrustedAsync(userId, deviceFingerprint)
            };

            if (session.IsTrustedDevice) { session.TrustedAt = DateTime.UtcNow; }

            var createdSession = await userSessionRepository.CreateAsync(session);

            logger.LogInformation("Created session {SessionId} for user {UserId} from {IpAddress}", createdSession.Id, userId, ipAddress);

            return createdSession;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session for user {UserId}", userId);

            throw;
        }
    }

    public async Task<UserSession?> GetSessionAsync(Guid sessionId)
    {
        var session = await userSessionRepository.GetByIdAsync(sessionId);
        return session?.IsActive == true ? session : null;
    }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        var session = await userSessionRepository.GetByRefreshTokenAsync(refreshToken);
        return session?.IsActive == true ? session : null;
    }

    public async Task<List<UserSession>> GetUserSessionsAsync(Guid userId, bool includeInactive = false)
    {
        if (includeInactive)
        {
            var allSessions = await userSessionRepository.GetAllByUserIdAsync(userId);
            return allSessions.ToList();
        }
        else
        {
            var activeSessions = await userSessionRepository.GetActiveByUserIdAsync(userId);
            return activeSessions.ToList();
        }
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        var session = await GetSessionAsync(sessionId);

        if (session == null || !session.IsValid) { return false; }

        // Update last used time
        session.LastUsedAt = DateTime.UtcNow;
        await userSessionRepository.UpdateAsync(session);

        return true;
    }

    public async Task<bool> RefreshSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);

            if (session == null || !session.IsValid) { return false; }

            // Extend session lifetime
            session.ExpiresAt = DateTime.UtcNow.Add(_options.SessionLifetime);
            session.LastUsedAt = DateTime.UtcNow;
            session.RefreshToken = GenerateRefreshToken(); // Generate new refresh token for security

            await userSessionRepository.UpdateAsync(session);

            logger.LogDebug("Refreshed session {SessionId} for user {UserId}", sessionId, session.UserId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh session {SessionId}", sessionId);

            return false;
        }
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId, SessionTerminationReason reason)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);

            if (session == null) { return false; }

            session.IsActive = false;
            session.TerminatedAt = DateTime.UtcNow;
            session.TerminationReason = reason.ToString();

            await userSessionRepository.UpdateAsync(session);

            logger.LogInformation("Terminated session {SessionId} for user {UserId} with reason {Reason}", sessionId, session.UserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to terminate session {SessionId}", sessionId);

            return false;
        }
    }

    public async Task<int> TerminateAllUserSessionsAsync(Guid userId, SessionTerminationReason reason, Guid? exceptSessionId = null)
    {
        try
        {
            var sessions = await userSessionRepository.GetActiveByUserIdAsync(userId);

            var sessionList = sessions.ToList();
            if (exceptSessionId.HasValue)
            {
                sessionList = sessionList.Where(s => s.Id != exceptSessionId.Value).ToList();
            }
            foreach (var session in sessionList)
            {
                session.IsActive = false;
                session.TerminatedAt = DateTime.UtcNow;
                session.TerminationReason = reason.ToString();
                await userSessionRepository.UpdateAsync(session);
            }

            logger.LogInformation("Terminated {Count} sessions for user {UserId} with reason {Reason}", sessions.Count, userId, reason);

            return sessions.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to terminate all sessions for user {UserId}", userId);

            return 0;
        }
    }

    public async Task<bool> TrustDeviceAsync(Guid userId, string deviceFingerprint, string deviceName)
    {
        try
        {
            var existingDevice = await trustedDeviceRepository.GetByUserAndFingerprintAsync(userId, deviceFingerprint);

            if (existingDevice != null)
            {
                existingDevice.IsActive = true;
                existingDevice.TrustedAt = DateTime.UtcNow;
                existingDevice.DeviceName = deviceName;
                await trustedDeviceRepository.UpdateAsync(existingDevice);
            }
            else
            {
                var trustedDevice = new TrustedDevice
                {
                    UserId = userId,
                    DeviceFingerprint = deviceFingerprint,
                    DeviceName = deviceName,
                    DeviceInfo = "{}",
                    TrustedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true,
                    ExpiresAt = _options.TrustedDeviceLifetime.HasValue ? DateTime.UtcNow.Add(_options.TrustedDeviceLifetime.Value) : null
                };

                await trustedDeviceRepository.CreateAsync(trustedDevice);
            }

            logger.LogInformation("Trusted device {DeviceFingerprint} for user {UserId}", deviceFingerprint, userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to trust device for user {UserId}", userId);

            return false;
        }
    }

    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint)
    {
        return await trustedDeviceRepository.IsDeviceTrustedAsync(userId, deviceFingerprint);
    }

    public async Task<List<TrustedDevice>> GetTrustedDevicesAsync(Guid userId)
    {
        var devices = await trustedDeviceRepository.GetByUserIdAsync(userId, activeOnly: true);
        return devices.ToList();
    }

    public async Task<bool> RevokeTrustedDeviceAsync(Guid deviceId, Guid userId)
    {
        try
        {
            var device = await trustedDeviceRepository.GetByIdAsync(deviceId);

            if (device == null || device.UserId != userId) { return false; }

            device.IsActive = false;
            await trustedDeviceRepository.UpdateAsync(device);

            logger.LogInformation("Revoked trusted device {DeviceId} for user {UserId}", deviceId, userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke trusted device {DeviceId} for user {UserId}", deviceId, userId);

            return false;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredCount = await userSessionRepository.CleanupExpiredSessionsAsync();

            if (expiredCount > 0) { logger.LogInformation("Cleaned up {Count} expired sessions", expiredCount); }
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to cleanup expired sessions"); }
    }

    public async Task<SessionSecurityAnalysis> AnalyzeSessionSecurityAsync(Guid userId, string ipAddress, string userAgent)
    {
        try
        {
            var allSessions = await userSessionRepository.GetAllByUserIdAsync(userId);
            var recentSessions = allSessions.Where(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToList();

            var analysis = new SessionSecurityAnalysis
            {
                IsNewLocation = !recentSessions.Any(s => s.IpAddress == ipAddress),
                IsNewDevice = !recentSessions.Any(s => s.UserAgent == userAgent),
                RecentLocationCount = recentSessions.Select(s => s.IpAddress).Distinct().Count(),
                RecentDeviceCount = recentSessions.Select(s => s.UserAgent).Distinct().Count(),
                LastSeenAt = recentSessions.Where(s => s.IpAddress == ipAddress || s.UserAgent == userAgent).OrderByDescending(s => s.LastUsedAt).FirstOrDefault()?.LastUsedAt
            };

            analysis.RiskScore = CalculateRiskScore(analysis);

            return analysis;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze session security for user {UserId}", userId);

            return new SessionSecurityAnalysis { RiskScore = RiskLevel.Medium };
        }
    }

    private async Task EnforceMaxSessionsLimitAsync(Guid userId)
    {
        if (_options.MaxSessionsPerUser <= 0) return;

        var activeSessions = await GetUserSessionsAsync(userId, true);

        if (activeSessions.Count >= _options.MaxSessionsPerUser)
        {
            // Terminate oldest sessions
            var sessionsToTerminate = activeSessions.OrderBy(s => s.LastUsedAt).Take(activeSessions.Count - _options.MaxSessionsPerUser + 1).ToList();

            foreach (var session in sessionsToTerminate) { await TerminateSessionAsync(session.Id, SessionTerminationReason.MaxSessionsExceeded); }
        }
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    private string GenerateDeviceFingerprint(string ipAddress, string userAgent, DeviceInfo deviceInfo)
    {
        var input = $"{ipAddress}:{userAgent}:{deviceInfo.Browser}:{deviceInfo.Os}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        return Convert.ToBase64String(hash);
    }

    private DeviceInfo ParseDeviceInfo(string userAgent)
    {
        try
        {
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);

            return new DeviceInfo
            {
                Browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}",
                Os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}",
                Device = clientInfo.Device.Family != "Other" ? clientInfo.Device.Family : "Desktop"
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse user agent: {UserAgent}", userAgent);

            return new DeviceInfo { Browser = "Unknown", Os = "Unknown", Device = "Unknown" };
        }
    }

    private async Task<LocationInfo?> GetLocationFromIpAsync(string ipAddress)
    {
        // This would integrate with a geolocation service
        // For now, return null or basic info
        return await Task.FromResult<LocationInfo?>(null);
    }

    private RiskLevel CalculateRiskScore(SessionSecurityAnalysis analysis)
    {
        var score = 0;

        if (analysis.IsNewLocation) score += 2;
        if (analysis.IsNewDevice) score += 2;
        if (analysis.RecentLocationCount > 5) score += 1;
        if (analysis.RecentDeviceCount > 3) score += 1;
        if (analysis.LastSeenAt == null) score += 3;
        if (analysis.LastSeenAt < DateTime.UtcNow.AddDays(-90)) score += 2;

        return score switch
        {
            <= 2 => RiskLevel.Low,
            <= 5 => RiskLevel.Medium,
            _ => RiskLevel.High
        };
    }
}
