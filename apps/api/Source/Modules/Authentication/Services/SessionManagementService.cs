using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using Microsoft.Extensions.Options;
using UAParser;

namespace GameGuild.Modules.Authentication;

public class SessionManagementService(ApplicationDbContext context, ILogger<SessionManagementService> logger, IOptions<SessionOptions> options) : ISessionManagementService
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

            context.Set<UserSession>().Add(session);
            await context.SaveChangesAsync();

            logger.LogInformation("Created session {SessionId} for user {UserId} from {IpAddress}", session.Id, userId, ipAddress);

            return session;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session for user {UserId}", userId);

            throw;
        }
    }

    public async Task<UserSession?> GetSessionAsync(Guid sessionId) { return await context.Set<UserSession>().FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive); }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken) { return await context.Set<UserSession>().FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive); }

    public async Task<List<UserSession>> GetUserSessionsAsync(Guid userId, bool activeOnly = true)
    {
        var query = context.Set<UserSession>().Where(s => s.UserId == userId);

        if (activeOnly) { query = query.Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow); }

        return await query.OrderByDescending(s => s.LastUsedAt).ToListAsync();
    }

    public async Task<bool> ValidateSessionAsync(Guid sessionId)
    {
        var session = await GetSessionAsync(sessionId);

        if (session == null || !session.IsValid) { return false; }

        // Update last used time
        session.LastUsedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

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

            await context.SaveChangesAsync();

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

            await context.SaveChangesAsync();

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
            var query = context.Set<UserSession>().Where(s => s.UserId == userId && s.IsActive);

            if (exceptSessionId.HasValue) { query = query.Where(s => s.Id != exceptSessionId.Value); }

            var sessions = await query.ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.TerminatedAt = DateTime.UtcNow;
                session.TerminationReason = reason.ToString();
            }

            await context.SaveChangesAsync();

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
            var existingDevice = await context.Set<TrustedDevice>().FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint);

            if (existingDevice != null)
            {
                existingDevice.IsActive = true;
                existingDevice.TrustedAt = DateTime.UtcNow;
                existingDevice.DeviceName = deviceName;
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

                context.Set<TrustedDevice>().Add(trustedDevice);
            }

            await context.SaveChangesAsync();

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
        var trustedDevice = await context.Set<TrustedDevice>().FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceFingerprint && d.IsActive);

        return trustedDevice?.IsValid == true;
    }

    public async Task<List<TrustedDevice>> GetTrustedDevicesAsync(Guid userId)
    {
        return await context.Set<TrustedDevice>().Where(d => d.UserId == userId && d.IsActive).OrderByDescending(d => d.LastUsedAt).ToListAsync();
    }

    public async Task<bool> RevokeTrustedDeviceAsync(Guid userId, Guid deviceId)
    {
        try
        {
            var device = await context.Set<TrustedDevice>().FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

            if (device == null) { return false; }

            device.IsActive = false;
            await context.SaveChangesAsync();

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
            var expiredSessions = await context.Set<UserSession>().Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow).ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                session.TerminatedAt = DateTime.UtcNow;
                session.TerminationReason = SessionTerminationReason.Expired.ToString();
            }

            await context.SaveChangesAsync();

            if (expiredSessions.Count > 0) { logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count); }
        }
        catch (Exception ex) { logger.LogError(ex, "Failed to cleanup expired sessions"); }
    }

    public async Task<SessionSecurityAnalysis> AnalyzeSessionSecurityAsync(Guid userId, string ipAddress, string userAgent)
    {
        try
        {
            var recentSessions = await context.Set<UserSession>().Where(s => s.UserId == userId && s.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToListAsync();

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
                Browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}", Os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}", Device = clientInfo.Device.Family != "Other" ? clientInfo.Device.Family : "Desktop"
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
