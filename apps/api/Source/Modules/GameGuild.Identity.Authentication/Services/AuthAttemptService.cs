using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Login attempt recording and IP address extraction
/// </summary>
public class AuthAttemptService(
    IAuthenticationAttemptRepository authenticationAttemptRepository,
    IUserEnumerationProtectionService enumerationProtection,
    ILogger<AuthAttemptService> logger
) : IAuthAttemptService
{
    public async Task RecordSuccessfulAttemptAsync(string email, Guid userId, string ipAddress, string? userAgent, TimeSpan processingTime)
    {
        try
        {
            var attempt = new AuthenticationAttempt
            {
                Email = email,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = true,
                AttemptedAt = SystemClock.UtcNow,
                ProcessingTime = processingTime
            };

            await authenticationAttemptRepository.CreateAsync(attempt).ConfigureAwait(false);
            LogLoginAuditEvent(attempt);
        }
        catch (Exception ex)
        {
            // Don't throw - authentication succeeded even if logging failed
            logger.LogError(ex, "Error recording successful authentication attempt");
        }
    }

    public async Task RecordFailedAttemptAsync(string email, Guid? userId, string ipAddress, string? userAgent, string failureReason, TimeSpan processingTime)
    {
        try
        {
            var attempt = new AuthenticationAttempt
            {
                Email = email,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = false,
                FailureReason = failureReason,
                AttemptedAt = SystemClock.UtcNow,
                ProcessingTime = processingTime
            };

            await authenticationAttemptRepository.CreateAsync(attempt).ConfigureAwait(false);
            LogLoginAuditEvent(attempt);

            // Record enumeration attempt for throttling
            await enumerationProtection.RecordEnumerationAttemptAsync(ipAddress, "login").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Don't throw - this is just logging
            logger.LogError(ex, "Error recording failed authentication attempt");
        }
    }

    public string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return "Unknown";

        // Check for forwarded IP first (common in reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            var firstIp = forwardedFor.Split(',')[0].Trim();

            if (!string.IsNullOrEmpty(firstIp)) return firstIp;
        }

        // Check X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(realIp)) return realIp;

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void LogLoginAuditEvent(AuthenticationAttempt attempt)
    {
        var level = attempt.IsSuccessful ? LogLevel.Information : LogLevel.Warning;
        logger.Log(
            level,
            "Authentication audit event {AuditEventType}: UserId={UserId}, EmailHash={EmailHash}, TenantId={TenantId}, IpAddress={IpAddress}, Success={Success}, FailureReason={FailureReason}, ProcessingTimeMs={ProcessingTimeMs}, AuditEvent={AuditEvent}, AuditCategory={AuditCategory}",
            attempt.IsSuccessful ? "AuthenticationSucceeded" : "AuthenticationFailed",
            attempt.UserId,
            HashIdentifier(attempt.Email),
            attempt.TenantId,
            attempt.IpAddress,
            attempt.IsSuccessful,
            attempt.FailureReason ?? string.Empty,
            attempt.ProcessingTime.TotalMilliseconds,
            true,
            "Authentication");
    }

    private static string HashIdentifier(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }
}
