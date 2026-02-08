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
                AttemptedAt = DateTime.UtcNow,
                ProcessingTime = processingTime
            };

            await authenticationAttemptRepository.CreateAsync(attempt).ConfigureAwait(false);
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
                AttemptedAt = DateTime.UtcNow,
                ProcessingTime = processingTime
            };

            await authenticationAttemptRepository.CreateAsync(attempt).ConfigureAwait(false);

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
            var firstIp = forwardedFor.Split(',').FirstOrDefault()?.Trim();

            if (!string.IsNullOrEmpty(firstIp)) return firstIp;
        }

        // Check X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();

        if (!string.IsNullOrEmpty(realIp)) return realIp;

        // Fall back to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
