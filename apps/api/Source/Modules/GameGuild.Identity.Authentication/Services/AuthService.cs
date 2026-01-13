using System.Diagnostics;
using System.Globalization;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthenticationAttemptRepository authenticationAttemptRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenHasher refreshTokenHasher,
    IOAuthService oauthService,
    IConfiguration configuration,
    IWeb3Service web3Service,
#pragma warning disable CS9113 // Parameter is unread - reserved for future use
    IEmailVerificationService emailVerificationService,
    // TODO: Add when Tenant module is implemented
    // ITenantAuthService tenantAuthService,
    // ITenantService tenantService,
    // IPermissionService permissionService,
    IAuthenticationAnomalyDetectionService anomalyDetectionService,
#pragma warning restore CS9113
    IUserEnumerationProtectionService enumerationProtection,
    // TODO: Add when Audit module is implemented
    // IAuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger
) : IAuthService
{
    // Note: emailVerificationService and anomalyDetectionService are available via primary constructor for future use

    public async Task<SignInResponse> LocalSignInAsync(LocalSignInRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

        // TODO: Implement when User module is available
        // User? user = null;
#pragma warning disable IDE0059 // Unnecessary assignment - Initial null IS used in failure path at RecordFailedAttempt
        Guid? userId = null;
#pragma warning restore IDE0059
        var userExists = false;
        var authenticationSucceeded = false;
        string? failureReason = null;

        try
        {
            // Check for throttling first
            // TODO: Implement throttling check when anomaly detection service is ready
            // var throttleDecision = await anomalyDetectionService.ShouldThrottleAsync(ipAddress);
            // if (throttleDecision.ShouldThrottle) { ... }

            // Lookup user from database
            var normalizedEmail = request.Email.ToLowerInvariant();
            var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            userExists = user != null;

            // Verify password if user exists
            if (user != null)
            {
                // Verify password using BCrypt
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
            await enumerationProtection.AddTimingProtectionDelayAsync(userExists, DateTime.UtcNow);

            if (!authenticationSucceeded)
            {
                await RecordFailedAttempt(request.Email, userId, ipAddress, userAgent, failureReason!, stopwatch.Elapsed);

                throw new UnauthorizedAccessException(enumerationProtection.GetGenericErrorMessage("login"));
            }

            // TODO: Analyze user login patterns for additional security
            // if (userId.HasValue)
            // {
            //     var userAnalysis = await anomalyDetectionService.AnalyzeUserLoginPatternsAsync(userId.Value, ipAddress, userAgent);
            //     if (userAnalysis.IsNewLocation || userAnalysis.IsNewDevice)
            //     {
            //         logger.LogInformation("User login from new location/device: UserId={UserId}", userId.Value);
            //     }
            // }

            // Create device info for refresh token
            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

            // Fetch user again to get token version (user variable may be null at this point due to scoping)
            var authenticatedUser = await userRepository.GetByIdAsync(userId!.Value, cancellationToken);
            var tokenVersion = authenticatedUser?.TokenVersion ?? 1;

            // Create tokens and response
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(userId!.Value, request.Email, ["User"], request.TenantId, tokenVersion, cancellationToken);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId.Value, deviceInfo, cancellationToken);

            // Expiries
            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            // NOTE: Refresh token is already saved to database by JwtTokenService.GenerateRefreshTokenAsync
            // No need to create and save it again here

            // Record successful login attempt
            await RecordSuccessfulAttempt(request.Email, userId.Value, ipAddress, userAgent, stopwatch.Elapsed);

            // TODO: Publish sign-in event when event system is implemented
            // await mediator.Publish(new UserSignedInEvent(userId.Value, request.Email, "local", ipAddress, userAgent, DateTime.UtcNow));

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-in successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UserId = userId.Value,
                Email = request.Email,
                SessionId = Guid.NewGuid(), // TODO: Get actual session ID from refresh token
                TenantId = request.TenantId
            };
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw authentication failures as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication for {Email}", request.Email);

            await RecordFailedAttempt(request.Email, userId, ipAddress, userAgent, "SystemError", stopwatch.Elapsed);

            throw new UnauthorizedAccessException(enumerationProtection.GetGenericErrorMessage("login"));
        }
    }

    public async Task<SignInResponse> LocalSignUpAsync(LocalSignUpRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        logger.LogInformation("[DEBUG] LocalSignUpAsync called with Email: '{Email}', Username: '{Username}'", request.Email, request.Username);

        try
        {
            // Check for existing user
            var emailExists = await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

            if (emailExists)
            {
                await enumerationProtection.AddTimingProtectionDelayAsync(true, DateTime.UtcNow);
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
            await userRepository.AddAsync(newUser, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);
            var userId = newUser.Id;

            logger.LogInformation("Created new user with ID: {UserId} and Email: {Email}", userId, newUser.Email);

            // Create device info for refresh token
            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

            // Create tokens (new users have TokenVersion = 1)
            var accessToken = await jwtTokenService.GenerateAccessTokenAsync(userId, request.Email, ["User"], request.TenantId, newUser.TokenVersion, cancellationToken);
            var refreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            // NOTE: refreshToken is already saved to database by JwtTokenService.GenerateRefreshTokenAsync
            // No need to save it again here

            // Record successful registration
            await RecordSuccessfulAttempt(request.Email, userId, ipAddress, userAgent, stopwatch.Elapsed);

            // TODO: Publish sign-up event when event system is implemented
            // await mediator.Publish(new UserSignedUpEvent(...));

            // TODO: Log audit when Audit module is implemented
            // await auditService.LogAsync(new CreateAuditLogRequest {...});

            logger.LogInformation("User {Email} successfully signed up", request.Email);

            logger.LogInformation("[DEBUG] Creating SignInResponse - UserId: {UserId}, Email from request: '{Email}'", userId, request.Email);

            return new SignInResponse
            {
                Success = true,
                Message = "Sign-up successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UserId = userId,
                Email = request.Email,
                SessionId = Guid.NewGuid(), // TODO: Get actual session ID from refresh token
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
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        // Hash the incoming token to match against stored hash
        var hashedToken = refreshTokenHasher.HashToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenAsync(hashedToken);

        logger.LogInformation("🔥 [AUTHSERVICE] Repository lookup result - storedToken is null: {IsNull}", storedToken == null);

        if (storedToken == null || !storedToken.IsActive || storedToken.ExpiresAt <= DateTime.UtcNow)
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

        // TODO: Get user from User module
        // var user = await userService.GetByIdAsync(storedToken.UserId);
        // if (user == null) throw new UnauthorizedAccessException("User not found");

        // TEMPORARY: Mock user data
        var userId = storedToken.UserId;
        var userEmail = $"user{userId}@game-guild.com";

        // Create device info for refresh token
        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Test Device", DeviceType = "Web" };

        // Generate new tokens (token rotation)
        var accessToken = jwtTokenService.GenerateAccessToken(userId, userEmail, ["User"]);
        var newRefreshToken = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);

        // Calculate expiry times
        var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        // Revoke old token (token rotation for security)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByToken = newRefreshToken;
        await refreshTokenRepository.UpdateAsync(storedToken);

        // NOTE: newRefreshToken is already saved to database by JwtTokenService.GenerateRefreshTokenAsync
        // No need to save it again here

        logger.LogInformation("Refresh token rotated for user {UserId}", userId);

        return new SignInResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = userEmail,
            SessionId = Guid.NewGuid() // TODO: Get actual session ID from the newRefreshTokenEntity
        };
    }

    public async Task RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
    {
        // Hash the incoming token to match against stored hash
        var hashedToken = refreshTokenHasher.HashToken(token);
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(hashedToken);

        if (refreshToken == null || !refreshToken.IsActive) { throw new ArgumentException("Invalid token"); }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.UpdatedAt = DateTime.UtcNow;

        await refreshTokenRepository.UpdateAsync(refreshToken);
    }

    public async Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing GitHub OAuth sign-in");

        // Get user profile from GitHub using access token
        var githubUser = await oauthService.GetUserProfileAsync("github", request.AccessToken);

        // TODO: Integrate with User module to find or create user
        // var user = await FindOrCreateOAuthUserAsync(githubUser.Email, githubUser.Name, "github", githubUser.ProviderId);

        // TEMPORARY: Mock user creation
        var userId = Guid.NewGuid();
        var email = githubUser.Email ?? throw new UnauthorizedAccessException("Email not available from GitHub profile");
        var roles = new[] { "User" };

        // Get IP and user agent for device info
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        // Create device info for refresh token
        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays);

        // Create refresh token
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await refreshTokenRepository.CreateAsync(refreshToken);

        // TODO: Publish UserSignedInEvent via mediator when integrated

        logger.LogInformation("GitHub OAuth sign-in successful for {Email}", email);

        return new SignInResponse
        {
            Success = true,
            Message = "GitHub sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public async Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing Google OAuth sign-in");

        // Get user profile from Google using access token
        var googleUser = await oauthService.GetUserProfileAsync("google", request.AccessToken);

        // TODO: Integrate with User module to find or create user
        // var user = await FindOrCreateOAuthUserAsync(googleUser.Email, googleUser.Name, "google", googleUser.ProviderId);

        // TEMPORARY: Mock user creation
        var userId = Guid.NewGuid();
        var email = googleUser.Email ?? throw new UnauthorizedAccessException("Email not available from Google profile");
        var roles = new[] { "User" };

        // Extract device info
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays);

        // Create refresh token
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await refreshTokenRepository.CreateAsync(refreshToken);

        // TODO: Publish UserSignedInEvent via mediator when integrated

        logger.LogInformation("Google OAuth sign-in successful for {Email}", email);

        return new SignInResponse
        {
            Success = true,
            Message = "Google sign-in successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    /// <summary>
    ///     Sign in using Google ID Token (for NextAuth.js integration)
    /// </summary>
    public async Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate that we have an ID token
            if (string.IsNullOrEmpty(request.IdToken)) { throw new ArgumentException("ID token is required"); }

            // Validate Google ID Token
            var googleUser = await oauthService.ValidateIdTokenAsync("google", request.IdToken);

            var email = googleUser.Email ?? throw new UnauthorizedAccessException("Email not found in ID token");

            // Find or create user
            var user = await userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                // Create new user for OAuth sign-in using the unified User entity
                user = User.CreateOAuthUser(email, googleUser.Name ?? email.Split('@')[0]);

                await userRepository.AddAsync(user, cancellationToken);
                await userRepository.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Created new user from Google sign-in: {Email}", email);
            }

            var userId = user.Id;
            var roles = new[] { "User" };

            // Extract device info
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "OAuth Device", DeviceType = "Web" };

            // Generate tokens
            var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
            var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);

            // Calculate expiry times
            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            // Create refresh token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshTokenValue,
                ExpiresAt = refreshTokenExpiresAt,
                IsRevoked = false,
                CreatedByIp = ipAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await refreshTokenRepository.CreateAsync(refreshTokenEntity);

            // TODO: Publish UserSignedInEvent via mediator when integrated

            logger.LogInformation("Google ID token sign-in successful for {Email}", email);

            return new SignInResponse
            {
                Success = true,
                Message = "Google ID token sign-in successful",
                AccessToken = jwtToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = refreshTokenExpiresAt,
                ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UserId = userId,
                Email = email,
                SessionId = refreshTokenEntity.Id
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google ID token validation failed");

            throw new UnauthorizedAccessException($"Google ID token validation failed: {ex.Message}", ex);
        }
    }

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:GitHub:ClientId"];
        var scopes = "user:email";
        var state = Guid.NewGuid().ToString(); // In production, store this for validation

        var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scopes}&state={state}";

        return Task.FromResult(url);
    }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri)
    {
        var clientId = configuration["OAuth:Google:ClientId"];
        var scopes = "openid email profile";
        var state = Guid.NewGuid().ToString(); // In production, store this for validation

        var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}&response_type=code&state={state}";

        return Task.FromResult(url);
    }

    public async Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Web3 challenge for wallet {WalletAddress}", request.WalletAddress);

        var challenge = await web3Service.GenerateChallengeAsync(request.WalletAddress);

        return new Web3ChallengeResponse { Challenge = challenge.Message, ExpiresAt = challenge.ExpiresAt };
    }

    public async Task<SignInResponse> VerifyWeb3SignatureAsync(Web3VerificationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying Web3 signature for wallet {WalletAddress}", request.WalletAddress);

        // Verify the signature
        var isValid = await web3Service.VerifySignatureAsync(request.WalletAddress, request.Signature, request.Challenge);

        if (!isValid) { throw new UnauthorizedAccessException("Invalid Web3 signature"); }

        // TODO: Integrate with User module to find or create Web3 user
        // var user = await FindOrCreateWeb3UserAsync(request.WalletAddress);

        // TEMPORARY: Mock user creation
        var userId = Guid.NewGuid();
        var email = $"{request.WalletAddress.ToLowerInvariant()}@web3.local";
        var roles = new[] { "User" };

        // Extract device info
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        var deviceInfo = new DeviceInfo { Fingerprint = Guid.NewGuid().ToString(), IpAddress = ipAddress, UserAgent = userAgent, DeviceName = "Web3 Device", DeviceType = "Web" };

        var jwtToken = jwtTokenService.GenerateAccessToken(userId, email, roles);
        var refreshTokenValue = await jwtTokenService.GenerateRefreshTokenAsync(userId, deviceInfo, cancellationToken);
        var refreshExpiresInDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7", CultureInfo.InvariantCulture);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpiresInDays);

        // Create refresh token
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await refreshTokenRepository.CreateAsync(refreshToken);

        // TODO: Publish UserSignedInEvent via mediator when integrated

        logger.LogInformation("Web3 signature verified for wallet {WalletAddress}", request.WalletAddress);

        return new SignInResponse
        {
            Success = true,
            Message = "Web3 authentication successful",
            AccessToken = jwtToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt,
            ExpiresIn = (int)(refreshTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
            UserId = userId,
            Email = email,
            SessionId = refreshToken.Id
        };
    }

    public Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending email verification to {Email}", request.Email);

        // TODO: Integrate with User module to get userId
        // For now, return a placeholder response
        // In full implementation: var userId = await userService.GetByEmailAsync(request.Email);
        // var token = await emailVerificationService.GenerateVerificationTokenAsync(userId, request.Email);
        // await emailVerificationService.SendVerificationEmailAsync(request.Email, token);

        return Task.FromResult(new EmailOperationResponse { Success = true, Message = "Verification email sent successfully" });
    }

    public Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying email with token");

        // TODO: Integrate with User module to verify email
        // In full implementation: await emailVerificationService.VerifyEmailTokenAsync(userId, request.Token);

        return Task.FromResult(new EmailOperationResponse { Success = true, Message = "Email verified successfully" });
    }

    public Task<EmailOperationResponse> ForgotPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing forgot password request for {Email}", request.Email);

        // TODO: Integrate with User module to send password reset
        // In full implementation: var userId = await userService.GetByEmailAsync(request.Email);
        // var token = await emailVerificationService.GeneratePasswordResetTokenAsync(userId);
        // await emailVerificationService.SendPasswordResetEmailAsync(request.Email, token);

        return Task.FromResult(new EmailOperationResponse { Success = true, Message = "Password reset email sent successfully" });
    }

    public Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing password reset");

        // TODO: Integrate with User module to reset password
        // In full implementation: await emailVerificationService.ValidatePasswordResetTokenAsync(request.Token);
        // await userService.UpdatePasswordAsync(userId, request.NewPassword);

        return Task.FromResult(new EmailOperationResponse { Success = true, Message = "Password reset successfully" });
    }

    public Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing password change for user {UserId}", userId);

        // TODO: Integrate with User module to change password
        // In full implementation:
        // var user = await userService.GetByIdAsync(userId);
        // var passwordCredential = await credentialService.GetPasswordCredentialAsync(userId);
        // if (!await credentialService.VerifyPasswordAsync(passwordCredential, request.CurrentPassword))
        //     return new EmailOperationResponse { Success = false, Message = "Current password is incorrect" };
        // await credentialService.UpdatePasswordAsync(userId, request.NewPassword);

        return Task.FromResult(new EmailOperationResponse { Success = true, Message = "Password changed successfully" });
    }

    // TODO: Uncomment when User module is integrated
    /*
    private async Task<User> FindOrCreateOAuthUserAsync(string email, string name, string provider, string providerId)
    {
        // Normalize email
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Try to find existing user
        var user = await userRepository.GetByEmailAsync(normalizedEmail);

        if (user != null) { return user; }

        // Create new user
        var isNewUser = false;

        if (user == null)
        {
            var baseUsername = name.ToSlugCase();

            var existingUser = await userRepository.GetByUsernameAsync(baseUsername);

            var uniqueUsername = existingUser == null ? baseUsername : $"{baseUsername}{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            user = new User { Id = Guid.NewGuid(), Username = uniqueUsername, Email = email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, };

            await userRepository.AddAsync(user);

            isNewUser = true;
        }

        // Check if OAuth credential exists
        var credential = await credentialRepository.GetByUserIdAndTypeAsync(user.Id, provider);

        if (credential == null)
        {
            credential = new Credential
            {
                UserId = user.Id,
                Type = provider,
                Value = providerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await credentialRepository.AddAsync(credential);
        }

        return user;
    }

    private async Task<(User User, bool IsNewUser)> FindOrCreateOAuthUserWithInfoAsync(string email, string name, string provider, string providerId)
    {
        // Similar logic as above but returns tuple with isNewUser flag
        var user = await FindOrCreateOAuthUserAsync(email, name, provider, providerId);
        return (user, false); // Simplified - in real implementation track if user was created
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) { throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken)); }

        var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            IsRevoked = false,
            CreatedByIp = "0.0.0.0",
        };

        await refreshTokenRepository.CreateAsync(refreshTokenEntity);
    }
    */

    private string GetClientIpAddress(HttpContext? httpContext)
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

    private async Task RecordSuccessfulAttempt(string email, Guid userId, string ipAddress, string? userAgent, TimeSpan processingTime)
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
                ProcessingTime = processingTime,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticationAttemptRepository.CreateAsync(attempt);
        }
        catch (Exception ex)
        {
            // Don't throw - authentication succeeded even if logging failed
            logger.LogError(ex, "Error recording successful authentication attempt");
        }
    }

    private async Task RecordFailedAttempt(string email, Guid? userId, string ipAddress, string? userAgent, string failureReason, TimeSpan processingTime)
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
                ProcessingTime = processingTime,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticationAttemptRepository.CreateAsync(attempt);

            // Record enumeration attempt for throttling
            await enumerationProtection.RecordEnumerationAttemptAsync(ipAddress, "login");
        }
        catch (Exception ex)
        {
            // Don't throw - this is just logging
            logger.LogError(ex, "Error recording failed authentication attempt");
        }
    }
}
