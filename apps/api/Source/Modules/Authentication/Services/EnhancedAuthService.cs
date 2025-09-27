using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Audit;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Enhanced authentication service with anomaly detection and user enumeration protection
/// </summary>
public class EnhancedAuthService(
    ApplicationDbContext context,
    IJwtTokenService jwtTokenService,
    IOAuthService oauthService,
    IConfiguration configuration,
    IWeb3Service web3Service,
    IEmailVerificationService emailVerificationService,
    ITenantAuthService tenantAuthService,
    ITenantService tenantService,
    IAuthenticationAnomalyDetectionService anomalyDetectionService,
    IUserEnumerationProtectionService enumerationProtection,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EnhancedAuthService> logger
) : IAuthService
{
    private readonly IOAuthService _oauthService = oauthService;

    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;

    public async Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        User? user = null;
        var userExists = false;
        var authenticationSucceeded = false;
        string? failureReason = null;

        try
        {
            // Check for throttling first
            var throttleDecision = await anomalyDetectionService.ShouldThrottleAsync(ipAddress, request.Email);

            if (throttleDecision.ShouldThrottle)
            {
                await RecordFailedAttempt(request.Email, null, ipAddress, userAgent, AuthenticationFailureReasons.RateLimited, stopwatch.Elapsed, correlationId, request.TenantId);

                throw new UnauthorizedAccessException(enumerationProtection.GetConsistentErrorMessage());
            }

            // Lookup user
            var normalizedEmail = request.Email.ToLowerInvariant();
            user = await context.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            userExists = user != null;

            // Perform authentication
            if (userExists)
            {
                var passwordCredential = user!.Credentials.FirstOrDefault(c => c is { Type: "password", IsActive: true });

                if (passwordCredential != null && VerifyPassword(request.Password, passwordCredential.Value)) { authenticationSucceeded = true; }
                else { failureReason = AuthenticationFailureReasons.InvalidCredentials; }
            }
            else
            {
                failureReason = AuthenticationFailureReasons.InvalidCredentials;
                // Perform dummy password hashing to maintain consistent timing
                await enumerationProtection.PerformDummyPasswordHashAsync(request.Password);
            }

            // Apply user enumeration protection timing
            await enumerationProtection.SimulateAuthenticationDelayAsync(request.Email, userExists);

            if (!authenticationSucceeded)
            {
                await RecordFailedAttempt(request.Email, user?.Id, ipAddress, userAgent, failureReason!, stopwatch.Elapsed, correlationId, request.TenantId);

                throw new UnauthorizedAccessException(enumerationProtection.GetConsistentErrorMessage());
            }

            // Analyze user login patterns for additional security
            if (user != null)
            {
                var userAnalysis = await anomalyDetectionService.AnalyzeUserLoginPatternsAsync(user.Id, ipAddress, userAgent);

                // Log suspicious patterns but don't block (could be legitimate new device/location)
                if (userAnalysis.IsNewLocation || userAnalysis.IsNewDevice)
                {
                    logger.LogInformation("User login from new location/device: UserId={UserId}, NewLocation={NewLocation}, NewDevice={NewDevice}", user.Id, userAnalysis.IsNewLocation, userAnalysis.IsNewDevice);
                }
            }

            // Create tokens and response
            var userDto = new UserDto { Id = user!.Id, Username = user.Name, Email = user.Email };
            var roles = new[ ] { "User" }; // TODO: fetch actual roles if available

            var accessToken = jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            // Expiries
            var accessTokenExpiryMinutes = int.Parse(configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);
            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            var refreshTokenEntity = new RefreshToken { UserId = user.Id, Token = refreshToken, ExpiresAt = refreshTokenExpiresAt, IsRevoked = false, CreatedByIp = ipAddress };

            context.RefreshTokens.Add(refreshTokenEntity);
            await context.SaveChangesAsync();

            // Record successful login attempt
            await RecordSuccessfulAttempt(request.Email, user.Id, ipAddress, userAgent, stopwatch.Elapsed, correlationId, request.TenantId);

            var response = new SignInResponse
            {
                AccessToken = accessToken, RefreshToken = refreshToken, ExpiresAt = refreshTokenExpiresAt, AccessTokenExpiresAt = accessTokenExpiresAt, RefreshTokenExpiresAt = refreshTokenExpiresAt, User = userDto
            };

            // Enhance response with tenant data
            return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authentication failures as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication for {Email}", request.Email);

            await RecordFailedAttempt(request.Email, user?.Id, ipAddress, userAgent, "SystemError", stopwatch.Elapsed, correlationId, request.TenantId);

            throw new UnauthorizedAccessException(enumerationProtection.GetConsistentErrorMessage());
        }
    }

    public async Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        try
        {
            // Check for existing user
            if (await context.Users.AnyAsync(u => u.Email == request.Email))
            {
                // Apply consistent timing even for existing users
                await enumerationProtection.SimulateAuthenticationDelayAsync(request.Email, true);

                throw new InvalidOperationException("User already exists");
            }

            // Create new user
            var user = new User { Name = request.Username ?? request.Email, Email = request.Email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var credential = new Credential { UserId = user.Id, Type = "password", Value = HashPassword(request.Password), IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            context.Credentials.Add(credential);
            await context.SaveChangesAsync();

            // Handle tenant association
            if (request.TenantId.HasValue)
            {
                try { await tenantService.AddUserToTenantAsync(user.Id, request.TenantId.Value); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to add user {UserId} to tenant {TenantId}", user.Id, request.TenantId); }
            }

            // Create tokens
            var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };
            var roles = new[ ] { "User" };

            var accessToken = jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            var accessTokenExpiryMinutes = int.Parse(configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);
            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            var refreshTokenEntity = new RefreshToken { UserId = user.Id, Token = refreshToken, ExpiresAt = refreshTokenExpiresAt, IsRevoked = false, CreatedByIp = ipAddress };

            context.RefreshTokens.Add(refreshTokenEntity);
            await context.SaveChangesAsync();

            // Record successful registration as login attempt
            await RecordSuccessfulAttempt(request.Email, user.Id, ipAddress, userAgent, stopwatch.Elapsed, correlationId, request.TenantId);

            // Log user creation audit
            await auditService.LogAsync(
                new CreateAuditLogRequest
                {
                    ActionType = AuditActionTypes.UserCreated,
                    ResourceType = "User",
                    ResourceId = user.Id.ToString(),
                    UserId = user.Id,
                    TenantId = request.TenantId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Description = $"User account created for {request.Email}",
                    Success = true,
                    Category = AuditCategory.Authentication,
                    CorrelationId = correlationId
                }
            );

            var response = new SignInResponse
            {
                AccessToken = accessToken, RefreshToken = refreshToken, ExpiresAt = refreshTokenExpiresAt, AccessTokenExpiresAt = accessTokenExpiresAt, RefreshTokenExpiresAt = refreshTokenExpiresAt, User = userDto
            };

            return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during user registration for {Email}", request.Email);

            throw;
        }
    }

    // Implement other IAuthService methods by delegating to original AuthService or implementing with security enhancements
    public Task<SignInResponse> GoogleSignInAsync(GoogleSignInRequestDto request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced Google sign-in not yet implemented");
    }

    public async Task<SignInResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Light logging only (avoid dumping all tokens in production)
        logger.LogInformation("Processing refresh token (len={Len})", request.RefreshToken?.Length);

        if (string.IsNullOrWhiteSpace(request.RefreshToken)) throw new UnauthorizedAccessException("Invalid refresh token");

        // Security enhancement: Get IP address for anomaly detection
        var ipAddress = GetClientIpAddress(httpContextAccessor.HttpContext);

        // We make refresh rotation idempotent: if two parallel calls try to rotate the same
        // token, only the first will create a new token; the others will detect the existing
        // replacement and return it instead of failing / creating multiple chains.
        const int maxAttempts = 2; // initial try + one concurrency fallback

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Load existing token inside loop (may change after concurrency failure)
                var existing = await context.RefreshTokens.Where(rt => rt.Token == request.RefreshToken).FirstOrDefaultAsync();

                if (existing == null)
                {
                    logger.LogWarning("Refresh token rejected (not found) from IP: {IpAddress}", ipAddress);

                    // Security enhancement: Record anomaly for token not found
                    await anomalyDetectionService.RecordLoginAttemptAsync(
                        new CreateAuthenticationAttemptRequest
                        {
                            Email = "unknown",
                            UserId = null,
                            IpAddress = ipAddress,
                            UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
                            IsSuccessful = false,
                            FailureReason = "Invalid refresh token"
                        }
                    );

                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                // If already rotated by another request: return replacement if still active
                if (existing.IsRevoked && existing.ReplacedByToken is not null)
                {
                    var replacement = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == existing.ReplacedByToken);

                    if (replacement != null && !replacement.IsRevoked && replacement.ExpiresAt > DateTime.UtcNow)
                    {
                        logger.LogInformation("Refresh token already rotated by another request (attempt {Attempt})", attempt);

                        var userAlready = await context.Users.FindAsync(existing.UserId) ?? throw new UnauthorizedAccessException("User not found");
                        var userDtoAlready = new UserDto { Id = userAlready.Id, Username = userAlready.Name, Email = userAlready.Email };
                        var rolesAlready = new[ ] { "User" }; // TODO: actual roles

                        var accessMinutesAlready = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? configuration["Jwt:ExpiryInMinutes"] ?? "60");
                        var newAccessTokenAlready = jwtTokenService.GenerateAccessToken(userDtoAlready, rolesAlready);
                        var newAccessTokenExpiresAtAlready = DateTime.UtcNow.AddMinutes(accessMinutesAlready);

                        var responseAlready = new SignInResponse
                        {
                            AccessToken = newAccessTokenAlready,
                            RefreshToken = replacement.Token,
                            ExpiresAt = replacement.ExpiresAt,
                            AccessTokenExpiresAt = newAccessTokenExpiresAtAlready,
                            RefreshTokenExpiresAt = replacement.ExpiresAt,
                            User = userDtoAlready,
                        };
                        responseAlready = await tenantAuthService.EnhanceWithTenantDataAsync(responseAlready, userAlready, request.TenantId);

                        return responseAlready;
                    }
                }

                if (existing.IsRevoked || existing.ExpiresAt <= DateTime.UtcNow)
                {
                    logger.LogWarning("Refresh token rejected (revoked / expired) from IP: {IpAddress}", ipAddress);

                    // Security enhancement: Record anomaly for revoked/expired token
                    await anomalyDetectionService.RecordLoginAttemptAsync(
                        new CreateAuthenticationAttemptRequest
                        {
                            Email = "unknown",
                            UserId = existing.UserId,
                            IpAddress = ipAddress,
                            UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
                            IsSuccessful = false,
                            FailureReason = existing.IsRevoked ? "Token revoked" : "Token expired"
                        }
                    );

                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                var user = await context.Users.FindAsync(existing.UserId) ?? throw new UnauthorizedAccessException("User not found");

                var tenantId = request.TenantId; // optional override
                IEnumerable<Claim>? tenantClaims = null;

                if (tenantId.HasValue)
                {
                    var permittedTenants = await tenantAuthService.GetUserTenantsAsync(user);

                    if (permittedTenants.Any(t => t.TenantId.HasValue && t.TenantId.Value == tenantId.Value)) { tenantClaims = await tenantAuthService.GetTenantClaimsAsync(user, tenantId.Value); }
                    else
                    {
                        tenantId = null; // ignore inaccessible tenant
                    }
                }

                // Config
                var accessMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? configuration["Jwt:ExpiryInMinutes"] ?? "60");
                var refreshDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

                var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };
                var roles = new[ ] { "User" }; // TODO: actual roles

                var newAccessToken = jwtTokenService.GenerateAccessToken(userDto, roles, tenantClaims);
                var newRefreshTokenValue = jwtTokenService.GenerateRefreshToken();
                var newAccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);
                var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);

                // Rotate (mark revoked)
                existing.IsRevoked = true;
                existing.RevokedAt = DateTime.UtcNow;
                existing.ReplacedByToken = newRefreshTokenValue;

                // Persist new refresh token
                var newRefreshTokenEntity = new RefreshToken { UserId = user.Id, Token = newRefreshTokenValue, ExpiresAt = newRefreshTokenExpiresAt, CreatedByIp = ipAddress, IsRevoked = false };
                context.RefreshTokens.Add(newRefreshTokenEntity);

                // Maintenance
                var cutoff = DateTime.UtcNow.AddDays(-30);
                var stale = await context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.ExpiresAt < cutoff).ToListAsync();
                if (stale.Count > 0) context.RefreshTokens.RemoveRange(stale);

                await context.SaveChangesAsync();

                // Security enhancement: Record successful token refresh
                await anomalyDetectionService.RecordLoginAttemptAsync(
                    new CreateAuthenticationAttemptRequest
                    {
                        Email = user.Email,
                        UserId = user.Id,
                        IpAddress = ipAddress,
                        UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
                        IsSuccessful = true,
                        FailureReason = null
                    }
                );

                var signInResponse = new SignInResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshTokenValue,
                    ExpiresAt = newRefreshTokenExpiresAt,
                    AccessTokenExpiresAt = newAccessTokenExpiresAt,
                    RefreshTokenExpiresAt = newRefreshTokenExpiresAt,
                    User = userDto,
                    TenantId = tenantId,
                };
                signInResponse = await tenantAuthService.EnhanceWithTenantDataAsync(signInResponse, user, tenantId);

                return signInResponse;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Concurrency conflict rotating refresh token (attempt {Attempt}) - retrying", attempt);
                // Clear tracked entities to avoid stale state before retry
                foreach (var entry in context.ChangeTracker.Entries().ToList()) entry.State = EntityState.Detached;
                await Task.Delay(25); // small backoff

                continue; // retry loop
            }
        }

        // If we reach here, concurrency did not resolve
        logger.LogError("Failed to rotate refresh token after {Attempts} attempts", maxAttempts);

        throw new UnauthorizedAccessException("Could not refresh token at this time");
    }

    public Task RevokeTokenAsync(RevokeRefreshTokenRequest request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced token revocation not yet implemented");
    }

    public Task<string> RequestPasswordResetAsync(PasswordResetRequestDto request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced password reset not yet implemented");
    }

    public Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced password reset confirmation not yet implemented");
    }

    public async Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request)
    {
        // Security enhancement: Get IP address for monitoring
        var ipAddress = GetClientIpAddress(httpContextAccessor.HttpContext);
        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown";

        logger.LogInformation("Web3 challenge request from IP: {IpAddress}, UserAgent: {UserAgent}, Address: {WalletAddress}", ipAddress, userAgent, request.WalletAddress);

        try
        {
            // Delegate to the Web3 service for challenge generation
            var challengeResponse = await web3Service.GenerateChallengeAsync(request);

            // Security enhancement: Record successful challenge generation
            await anomalyDetectionService.RecordLoginAttemptAsync(
                new CreateAuthenticationAttemptRequest { Email = "web3-challenge", UserId = null, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = true, FailureReason = null }
            );

            return challengeResponse;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid Web3 challenge request from IP: {IpAddress}, Error: {Error}", ipAddress, ex.Message);

            // Security enhancement: Record failed challenge generation for invalid addresses
            await anomalyDetectionService.RecordLoginAttemptAsync(
                new CreateAuthenticationAttemptRequest { Email = "web3-challenge-invalid", UserId = null, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = false, FailureReason = ex.Message }
            );

            throw; // Re-throw the original exception to maintain proper error handling
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Web3 challenge from IP: {IpAddress}", ipAddress);

            // Security enhancement: Record unexpected errors
            await anomalyDetectionService.RecordLoginAttemptAsync(
                new CreateAuthenticationAttemptRequest { Email = "web3-challenge-error", UserId = null, IpAddress = ipAddress, UserAgent = userAgent, IsSuccessful = false, FailureReason = "Challenge generation error" }
            );

            throw; // Re-throw the original exception
        }
    }

    public Task<SignInResponse> Web3SignInAsync(Web3SignInRequest request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced Web3 sign-in not yet implemented");
    }

    public Task<string> SendVerificationEmailAsync(SendVerificationEmailRequestDto request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced email verification not yet implemented");
    }

    public Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced email verification confirmation not yet implemented");
    }

    public Task<string> GetGitHubSignInUrlAsync(string redirectUri)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced GitHub sign-in not yet implemented");
    }

    public Task<SignInResponse> GitHubCallbackAsync(GitHubCallbackRequestDto request)
    {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced GitHub callback not yet implemented");
    }

    private async Task RecordSuccessfulAttempt(string email, Guid userId, string ipAddress, string? userAgent, TimeSpan processingTime, string? correlationId, Guid? tenantId)
    {
        var deviceFingerprint = anomalyDetectionService.GenerateDeviceFingerprint(userAgent);

        await anomalyDetectionService.RecordLoginAttemptAsync(
            new CreateAuthenticationAttemptRequest
            {
                Email = email,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = true,
                ProcessingTime = processingTime,
                DeviceFingerprint = deviceFingerprint,
                TenantId = tenantId,
                CorrelationId = correlationId
            }
        );

        await auditService.LogAsync(
            new CreateAuditLogRequest
            {
                ActionType = AuditActionTypes.Login,
                ResourceType = "User",
                ResourceId = userId.ToString(),
                UserId = userId,
                TenantId = tenantId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = $"Successful login for {email}",
                Success = true,
                Category = AuditCategory.Authentication,
                CorrelationId = correlationId
            }
        );
    }

    private async Task RecordFailedAttempt(string email, Guid? userId, string ipAddress, string? userAgent, string failureReason, TimeSpan processingTime, string? correlationId, Guid? tenantId)
    {
        var deviceFingerprint = anomalyDetectionService.GenerateDeviceFingerprint(userAgent);

        await anomalyDetectionService.RecordLoginAttemptAsync(
            new CreateAuthenticationAttemptRequest
            {
                Email = email,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = false,
                FailureReason = failureReason,
                ProcessingTime = processingTime,
                DeviceFingerprint = deviceFingerprint,
                TenantId = tenantId,
                CorrelationId = correlationId
            }
        );

        await auditService.LogAsync(
            new CreateAuditLogRequest
            {
                ActionType = AuditActionTypes.LoginFailed,
                ResourceType = "User",
                ResourceId = userId?.ToString(),
                UserId = userId,
                TenantId = tenantId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = $"Failed login attempt for {email}: {failureReason}",
                Success = false,
                ErrorMessage = failureReason,
                Category = AuditCategory.Security,
                CorrelationId = correlationId
            }
        );
    }

    private string GetClientIpAddress(HttpContext? context)
    {
        if (context == null) return "0.0.0.0";

        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrEmpty(ipAddress) || "unknown".Equals(ipAddress, StringComparison.OrdinalIgnoreCase)) { ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault(); }

        if (string.IsNullOrEmpty(ipAddress) || "unknown".Equals(ipAddress, StringComparison.OrdinalIgnoreCase)) { ipAddress = context.Connection.RemoteIpAddress?.ToString(); }

        return ipAddress ?? "0.0.0.0";
    }

    private static string HashPassword(string password)
    {
        // Use BCrypt for proper password hashing (replace the simple SHA256)
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor : 12);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch
        {
            // Fallback to old SHA256 method for existing passwords
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sha256Hash = Convert.ToBase64String(bytes);

            return sha256Hash == hash;
        }
    }

    // Missing interface method implementations (stubs)
    public Task RevokeRefreshTokenAsync(string refreshToken, string reason) { throw new NotImplementedException("RevokeRefreshTokenAsync not yet implemented"); }

    public Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request) { throw new NotImplementedException("GitHubSignInAsync not yet implemented"); }

    public Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request) { throw new NotImplementedException("GoogleSignInAsync not yet implemented"); }

    public Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequestDto request) { throw new NotImplementedException("GoogleIdTokenSignInAsync not yet implemented"); }

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri) { throw new NotImplementedException("GetGitHubAuthUrlAsync not yet implemented"); }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri) { throw new NotImplementedException("GetGoogleAuthUrlAsync not yet implemented"); }

    public Task<SignInResponse> VerifyWeb3SignatureAsync(Web3AuthenticationVerificationRequest request) { throw new NotImplementedException("VerifyWeb3SignatureAsync not yet implemented"); }

    public Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request) { throw new NotImplementedException("SendEmailVerificationAsync not yet implemented"); }

    public Task<EmailOperationResponse> ForgotPasswordAsync(ForgotPasswordRequestDto request) { throw new NotImplementedException("ForgotPasswordAsync not yet implemented"); }

    public Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId) { throw new NotImplementedException("ChangePasswordAsync not yet implemented"); }
}
