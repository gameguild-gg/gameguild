using System.Diagnostics;
using System.Globalization;
using GameGuild.CQRS;
using GameGuild.Email;
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
    ILogger<LocalAuthService> logger,
    IPublisher publisher,
    ISender sender
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
                var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

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
            var tenantAccessContext = await ResolveTenantAccessContextAsync(userId.Value, request.TenantId, cancellationToken).ConfigureAwait(false);
            RequireActiveTenantAccess(tenantAccessContext);

            // Create tokens and response
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(
                userId.Value,
                authenticatedUser?.Email ?? request.Email,
                tenantAccessContext.Roles.ToArray(),
                tenantAccessContext.TenantId,
                tokenVersion,
                cancellationToken).ConfigureAwait(false);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId.Value, deviceInfo, cancellationToken).ConfigureAwait(false);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

            // Record successful login attempt
            await authAttemptService.RecordSuccessfulAttemptAsync(request.Email, userId.Value, ipAddress, userAgent, stopwatch.Elapsed).ConfigureAwait(false);

            var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-in successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = accessTokenExpirationMinutes * 60,
                AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                UserId = userId.Value,
                Email = authenticatedUser?.Email ?? request.Email,
                SessionId = Guid.NewGuid(),
                TenantId = tenantAccessContext.TenantId,
                AvailableTenants = tenantAccessContext.AvailableTenants
            };
        }
        catch (SecurityException)
        {
            throw;
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

            await DefaultTenantMembershipProvisioner.EnsureAsync(sender, userId, cancellationToken).ConfigureAwait(false);

            // Create device info for refresh token
            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

            var tenantAccessContext = await ResolveTenantAccessContextAsync(userId, request.TenantId, cancellationToken).ConfigureAwait(false);

            // Create tokens (new users have TokenVersion = 1)
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(
                userId,
                newUser.Email,
                tenantAccessContext.Roles.ToArray(),
                tenantAccessContext.TenantId,
                newUser.TokenVersion,
                cancellationToken).ConfigureAwait(false);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken).ConfigureAwait(false);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = SystemClock.UtcNow.AddDays(refreshTokenExpiryDays);

            // Record successful registration
            await authAttemptService.RecordSuccessfulAttemptAsync(request.Email, userId, ipAddress, userAgent, stopwatch.Elapsed).ConfigureAwait(false);

            await publisher.Publish(
                new UserSignedUpNotification
                {
                    UserId = newUser.Id,
                    Email = newUser.Email,
                    Username = newUser.Username ?? newUser.Email,
                    TenantId = request.TenantId
                },
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("User {Email} successfully signed up", request.Email);

            var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60", CultureInfo.InvariantCulture);

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-up successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = accessTokenExpirationMinutes * 60,
                AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                UserId = userId,
                Email = newUser.Email,
                SessionId = Guid.NewGuid(),
                TenantId = tenantAccessContext.TenantId,
                AvailableTenants = tenantAccessContext.AvailableTenants
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
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogWarning("Refresh token request rejected because the token was missing.");

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = authAttemptService.GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        // Hash the incoming token to match against stored hash
        var hashedToken = refreshTokenHasher.HashToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenAsync(hashedToken).ConfigureAwait(false);

        if (storedToken == null || !storedToken.IsActive || storedToken.ExpiresAt <= SystemClock.UtcNow)
        {
            logger.LogWarning(
                "Invalid refresh token attempt from {IpAddress}. TokenFound: {TokenFound}, IsActive: {IsActive}, ExpiresAt: {ExpiresAt}",
                ipAddress,
                storedToken != null,
                storedToken?.IsActive,
                storedToken?.ExpiresAt
            );

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var userId = storedToken.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var tokenVersion = user?.TokenVersion ?? 1;
        var tenantAccessContext = await ResolveTenantAccessContextAsync(userId, request.TenantId, cancellationToken).ConfigureAwait(false);
        var userEmail = user?.Email ?? $"user{userId}@game-guild.com";
        RequireActiveTenantAccess(tenantAccessContext);

        // Create device info for refresh token
        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

        // Generate new tokens (token rotation)
        var accessToken = await jwtTokenService.GenerateAccessTokenAsync(
            userId,
            userEmail,
            tenantAccessContext.Roles.ToArray(),
            tenantAccessContext.TenantId,
            tokenVersion,
            cancellationToken).ConfigureAwait(false);
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

        var accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

        return new SignInResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            AccessTokenExpiresAt = SystemClock.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            Email = userEmail,
            SessionId = Guid.NewGuid(),
            TenantId = tenantAccessContext.TenantId,
            AvailableTenants = tenantAccessContext.AvailableTenants
        };
    }

    private static TenantAccessContext RequireActiveTenantAccess(TenantAccessContext tenantAccessContext)
    {
        if (tenantAccessContext.TenantId.HasValue)
            return tenantAccessContext;

        throw new AccessDeniedException("Authenticated user has no active tenant membership.");
    }

    private async Task<TenantAccessContext> ResolveTenantAccessContextAsync(Guid userId, Guid? requestedTenantId, CancellationToken cancellationToken)
    {
        var memberships = await sender.Send(new global::GameGuild.Identity.Tenants.GetUserMembershipsQuery(userId), cancellationToken).ConfigureAwait(false);

        if (memberships.TotalCount == 0)
        {
            return new TenantAccessContext(null, null, ["User"]);
        }

        var activeMemberships = memberships.Memberships
            .Where(membership => membership.IsActive)
            .ToList();

        if (activeMemberships.Count == 0)
        {
            return new TenantAccessContext(null, null, ["User"]);
        }

        var availableTenants = activeMemberships
            .GroupBy(membership => membership.TenantId)
            .Select(group => group.First())
            .Select(membership => new global::GameGuild.TenantInfo(
                membership.TenantId,
                membership.TenantName,
                membership.TenantSlug,
                membership.TenantIsActive))
            .ToList();

        var selectedTenantId = requestedTenantId.HasValue
            ? availableTenants.FirstOrDefault(tenant => tenant.Id == requestedTenantId.Value && tenant.IsActive)?.Id
            : null;

        selectedTenantId ??= availableTenants.FirstOrDefault(tenant => tenant.IsActive)?.Id;
        selectedTenantId ??= availableTenants[0].Id;

        var roles = activeMemberships
            .Where(membership => membership.TenantId == selectedTenantId)
            .Select(membership => membership.Role)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Append("User")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TenantAccessContext(selectedTenantId, availableTenants, roles);
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

    private sealed record TenantAccessContext(
        Guid? TenantId,
        IReadOnlyList<global::GameGuild.TenantInfo>? AvailableTenants,
        IReadOnlyList<string> Roles);
}
