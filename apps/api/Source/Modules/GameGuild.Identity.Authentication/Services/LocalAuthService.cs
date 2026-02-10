using System.Diagnostics;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Local authentication: sign-in, sign-up, refresh token rotation, token revocation
/// </summary>
public class LocalAuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenHasher refreshTokenHasher,
    IConfiguration configuration,
    IAuthAttemptService authAttemptService,
#pragma warning disable CS9113 // Parameter is unread - reserved for future use
    IAuthenticationAnomalyDetectionService anomalyDetectionService,
#pragma warning restore CS9113
    IUserEnumerationProtectionService enumerationProtection,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalAuthService> logger
) : ILocalAuthService
{
    public async Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

#pragma warning disable IDE0059 // Unnecessary assignment - Initial null IS used in failure path at RecordFailedAttempt
        Guid? userId = null;
#pragma warning restore IDE0059
        var userExists = false;
        var authenticationSucceeded = false;
        string? failureReason = null;

        try
        {
            // Lookup user from database
            var normalizedEmail = request.Email.ToLowerInvariant();
            var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
            userExists = user != null;

            // Verify password if user exists
            if (user != null)
            {
                var hashPreview = user.PasswordHash?.Substring(0, Math.Min(20, user.PasswordHash.Length)) ?? string.Empty;
                logger.LogInformation("DEBUG: Verifying password for user {Email}. Password length: {PasswordLength}, Hash: {Hash}", user.Email, request.Password.Length, hashPreview);

                var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                logger.LogInformation("DEBUG: Password verification result for {Email}: {IsValid}", user.Email, passwordValid);

                if (passwordValid)
                {
                    authenticationSucceeded = true;
                    userId = user.Id;
                    logger.LogInformation("User {Email} authenticated successfully with ID {UserId}", user.Email, userId);
                }
                else
                {
                    failureReason = "InvalidCredentials";
                    logger.LogWarning("Invalid password for user {Email}", request.Email);
                }
            }
            else
            {
                failureReason = "InvalidCredentials";
                logger.LogWarning("User not found: {Email}", request.Email);
            }

            // Apply user enumeration protection timing
            await enumerationProtection.AddTimingProtectionDelayAsync(userExists, SystemClock.UtcNow).ConfigureAwait(false);

            if (!authenticationSucceeded)
            {
                await authAttemptService.RecordFailedAttemptAsync(request.Email, userId, ipAddress, userAgent, failureReason!, stopwatch.Elapsed).ConfigureAwait(false);

                throw new UnauthorizedAccessException(enumerationProtection.GetGenericErrorMessage("login"));
            }

            // Analyze login attempt for anomalies
            var attemptContext = new AuthenticationAttemptContext
            {
                UserId = userId!.Value,
                IpAddress = ipAddress,
                UserAgent = userAgent ?? "Unknown",
                DeviceFingerprint = httpContextAccessor.HttpContext?.Request.Headers["X-Device-Fingerprint"].FirstOrDefault(), // Extracted from request header
                Timestamp = SystemClock.UtcNow
            };

            var anomalyResult = await anomalyDetectionService.AnalyzeLoginAttemptAsync(attemptContext).ConfigureAwait(false);

            // Require step-up authentication for high-risk logins
            if (anomalyResult.RiskLevel >= RiskLevel.High)
            {
                logger.LogWarning("High-risk login attempt detected: UserId={UserId}, RiskLevel={RiskLevel}, Anomalies={Anomalies}",
                    userId.Value, anomalyResult.RiskLevel, string.Join(", ", anomalyResult.DetectedAnomalies));

                var stepUpToken = Guid.NewGuid().ToString("N");
                var stepUpExpiresAt = SystemClock.UtcNow.AddMinutes(5);

                return new SignInResponse
                {
                    Success = false,
                    Message = "Additional verification required",
                    RequiresStepUp = true,
                    StepUpToken = stepUpToken,
                    StepUpExpiresAt = stepUpExpiresAt,
                    RiskLevel = anomalyResult.RiskLevel,
                    RiskFactors = anomalyResult.DetectedAnomalies.ToList(),
                    AvailableMethods = ["TOTP", "Email"],
                    UserId = userId.Value,
                    Email = request.Email,
                    TenantId = request.TenantId
                };
            }

            // Create device info for refresh token
            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

            // Fetch user again to get token version
            var authenticatedUser = await userRepository.GetByIdAsync(userId!.Value, cancellationToken).ConfigureAwait(false);
            var tokenVersion = authenticatedUser?.TokenVersion ?? 1;

            // Create tokens and response
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(userId!.Value, request.Email, ["User"], request.TenantId, tokenVersion, cancellationToken).ConfigureAwait(false);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId.Value, deviceInfo, cancellationToken).ConfigureAwait(false);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

            // Record successful login attempt
            await authAttemptService.RecordSuccessfulAttemptAsync(request.Email, userId.Value, ipAddress, userAgent, stopwatch.Elapsed).ConfigureAwait(false);

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-in successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - SystemClock.UtcNow).TotalSeconds,
                UserId = userId.Value,
                Email = request.Email,
                SessionId = Guid.NewGuid(),
                TenantId = request.TenantId
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication for {Email}", request.Email);

            await authAttemptService.RecordFailedAttemptAsync(request.Email, userId, ipAddress, userAgent, "SystemError", stopwatch.Elapsed).ConfigureAwait(false);

            throw new UnauthorizedAccessException(enumerationProtection.GetGenericErrorMessage("login"));
        }
    }

    public async Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        logger.LogInformation("[DEBUG] LocalSignUpAsync called with Email: '{Email}', Username: '{Username}'", request.Email, request.Username);

        try
        {
            // Check for existing user
            var emailExists = await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken).ConfigureAwait(false);

            if (emailExists)
            {
                await enumerationProtection.AddTimingProtectionDelayAsync(true, SystemClock.UtcNow).ConfigureAwait(false);
                logger.LogWarning("Sign-up attempt with existing email: {Email}", request.Email);

                throw new InvalidOperationException("User already exists");
            }

            // Hash password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user using the unified User entity
            var newUser = User.CreateWithPassword(
                request.Email.ToLowerInvariant(),
                request.Username ?? request.Email.Split('@')[0],
                passwordHash);

            // Save to database
            await userRepository.AddAsync(newUser, cancellationToken).ConfigureAwait(false);
            await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var userId = newUser.Id;

            logger.LogInformation("Created new user with ID: {UserId} and Email: {Email}", userId, newUser.Email);

            // Create device info for refresh token
            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

            // Create tokens (new users have TokenVersion = 1)
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(userId, request.Email, ["User"], request.TenantId, newUser.TokenVersion, cancellationToken).ConfigureAwait(false);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

            // Record successful registration
            await authAttemptService.RecordSuccessfulAttemptAsync(request.Email, userId, ipAddress, userAgent, stopwatch.Elapsed).ConfigureAwait(false);

            logger.LogInformation("User {Email} successfully signed up", request.Email);

            logger.LogInformation("[DEBUG] Creating SignInResponse - UserId: {UserId}, Email from request: '{Email}'", userId, request.Email);

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-up successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - SystemClock.UtcNow).TotalSeconds,
                UserId = userId,
                Email = request.Email,
                SessionId = Guid.NewGuid(),
                TenantId = request.TenantId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during user registration for {Email}", request.Email);

            throw;
        }
    }

    public async Task<SignInResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("🔥 [AUTHSERVICE] Processing refresh token request: {RefreshToken}", request.RefreshToken);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogWarning("🔥 [AUTHSERVICE] RefreshToken is null or empty, throwing UnauthorizedAccessException");

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        // Hash the incoming token to match against stored hash
        var hashedToken = refreshTokenHasher.HashToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenAsync(hashedToken).ConfigureAwait(false);

        logger.LogInformation("🔥 [AUTHSERVICE] Repository lookup result - storedToken is null: {IsNull}", storedToken == null);

        if (storedToken == null || !storedToken.IsActive || storedToken.ExpiresAt <= SystemClock.UtcNow)
        {
            logger.LogWarning(
                "🔥 [AUTHSERVICE] Invalid refresh token attempt from {IpAddress}. storedToken is null: {IsNull}, IsActive: {IsActive}, ExpiresAt: {ExpiresAt}",
                ipAddress,
                storedToken == null,
                storedToken?.IsActive,
                storedToken?.ExpiresAt
            );

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var userId = storedToken.UserId;
        var userEmail = $"user{userId}@game-guild.com";

        // Create device info for refresh token
        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

        // Generate new tokens (token rotation)
        var accessToken = jwtTokenService.GenerateAccessToken(userId, userEmail, ["User"]);
        var newRefreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);

        var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
        var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

        // Revoke old token (token rotation for security)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = SystemClock.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByToken = newRefreshToken;
        await refreshTokenRepository.UpdateAsync(storedToken).ConfigureAwait(false);

        logger.LogInformation("Refresh token rotated for user {UserId}", userId);

        return new SignInResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - SystemClock.UtcNow).TotalSeconds,
            UserId = userId,
            Email = userEmail,
            SessionId = Guid.NewGuid()
        };
    }

    public async Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to match against stored hash
        var hashedToken = refreshTokenHasher.HashToken(token);
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(hashedToken).ConfigureAwait(false);

        if (refreshToken == null || !refreshToken.IsActive) { throw new ArgumentException("Invalid token"); }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = SystemClock.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.UpdatedAt = SystemClock.UtcNow;

        await refreshTokenRepository.UpdateAsync(refreshToken).ConfigureAwait(false);
    }
}
