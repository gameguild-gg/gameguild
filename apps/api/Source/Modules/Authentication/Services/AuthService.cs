using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Modules.Audit;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication
{
    public class AuthService(
        IUserRepository userRepository,
        ICredentialRepository credentialRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthenticationAttemptRepository authenticationAttemptRepository,
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
        ILogger<AuthService> logger
    ) : IAuthService
    {
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
                user = await userRepository.GetByEmailAsync(normalizedEmail);

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
                var userDto = new UserDto { Id = user!.Id, Username = user.Username, Email = user.Email };
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
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = refreshTokenExpiresAt,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt,
                    User = userDto
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

                await RecordFailedAttempt(request.Email, user?.Id, ipAddress, userAgent, AuthenticationFailureReasons.SystemError, stopwatch.Elapsed, correlationId, request.TenantId);

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
                var existingUser = await userRepository.GetByEmailAsync(request.Email.ToLowerInvariant());

                if (existingUser != null)
                {
                    // Apply consistent timing even for existing users
                    await enumerationProtection.SimulateAuthenticationDelayAsync(request.Email, true);

                    throw new InvalidOperationException("User already exists");
                }

                // Create new user
                var user = new User { Username = request.Username ?? request.Email, Email = request.Email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

                await userRepository.AddAsync(user);
                await userRepository.SaveChangesAsync();

                var credential = new Credential { UserId = user.Id, Type = "password", Value = HashPassword(request.Password), IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

                await credentialRepository.AddAsync(credential);
                await credentialRepository.SaveChangesAsync();

                // Handle tenant association
                if (request.TenantId.HasValue)
                {
                    // Note: AddUserToTenantAsync method not available in current ITenantService interface
                    // TODO: Implement tenant user association when interface is updated
                    logger.LogInformation("User {UserId} registered for tenant {TenantId}", user.Id, request.TenantId.Value);
                }

                // Create tokens
                var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email };
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
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = refreshTokenExpiresAt,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt,
                    User = userDto
                };

                return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during user registration for {Email}", request.Email);

                throw;
            }
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
                    var existing = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

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
                        var replacement = existing.ReplacedByToken != null ? await refreshTokenRepository.GetByTokenAsync(existing.ReplacedByToken) : null;

                        if (replacement != null && !replacement.IsRevoked && replacement.ExpiresAt > DateTime.UtcNow)
                        {
                            logger.LogInformation("Refresh token already rotated by another request (attempt {Attempt})", attempt);

                            var userAlready = await userRepository.GetByIdAsync(existing.UserId) ?? throw new UnauthorizedAccessException("User not found");
                            var userDtoAlready = new UserDto { Id = userAlready.Id, Username = userAlready.Username, Email = userAlready.Email };
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
                        logger.LogWarning("Refresh token rejected (revoked / expired)");

                        throw new UnauthorizedAccessException("Invalid refresh token");
                    }

                    var user = await userRepository.GetByIdAsync(existing.UserId) ?? throw new UnauthorizedAccessException("User not found");

                    var tenantId = request.TenantId; // optional override
                    IEnumerable<Claim>? tenantClaims = null;

                    if (tenantId.HasValue)
                    {
                        // Note: GetUserTenantsAsync method not available in current ITenantAuthService interface
                        // Using empty list until interface is updated
                        var permittedTenants = new List<object>();

                        if (permittedTenants.Any(t => t.TenantId.HasValue && t.TenantId.Value == tenantId.Value)) { tenantClaims = await tenantAuthService.GetTenantClaimsAsync(user, tenantId.Value); }
                        else
                        {
                            tenantId = null; // ignore inaccessible tenant
                        }
                    }

                    // Config
                    var accessMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? configuration["Jwt:ExpiryInMinutes"] ?? "60");
                    var refreshDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

                    var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email };
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
                    var newRefreshTokenEntity = new RefreshToken { UserId = user.Id, Token = newRefreshTokenValue, ExpiresAt = newRefreshTokenExpiresAt, CreatedByIp = "0.0.0.0", IsRevoked = false, };
                    await refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

                    // Note: Token cleanup handled by background service to avoid blocking user requests

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
                    // Context detachment handled by repository layer
                    await Task.Delay(25); // small backoff

                    continue; // retry loop
                }
            }

            // If we reach here, concurrency did not resolve
            logger.LogError("Failed to rotate refresh token after {Attempts} attempts", maxAttempts);

            throw new UnauthorizedAccessException("Could not refresh token at this time");
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress)
        {
            var refreshToken = await refreshTokenRepository.GetByTokenAsync(token);

            if (refreshToken == null || !refreshToken.IsActive) throw new ArgumentException("Invalid token");

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            await refreshTokenRepository.UpdateAsync(refreshToken);
        }

        public async Task<SignInResponse> GitHubSignInAsync(OAuthSignInRequest request)
        {
            // Exchange code for access token
            var accessToken = await oauthService.ExchangeGitHubCodeAsync(request.Code, request.RedirectUri);

            // Get user info from GitHub
            var githubUser = await oauthService.GetGitHubUserAsync(accessToken);

            // Find or create user
            var user = await FindOrCreateOAuthUserAsync(githubUser.Email, githubUser.Name, "github", githubUser.Id.ToString());

            // Generate tokens
            var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email, };
            var roles = new[ ] { "User", }; // TODO: fetch actual roles
            var jwtToken = jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponse { AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto, };

            // Enhance with tenant data
            return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        public async Task<SignInResponse> GoogleSignInAsync(OAuthSignInRequest request)
        {
            // Exchange code for access token
            var accessToken = await oauthService.ExchangeGoogleCodeAsync(request.Code, request.RedirectUri);

            // Get user info from Google
            var googleUser = await oauthService.GetGoogleUserAsync(accessToken);

            // Find or create user
            var user = await FindOrCreateOAuthUserAsync(googleUser.Email, googleUser.Name, "google", googleUser.Id);

            // Generate tokens
            var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email, };
            var roles = new[ ] { "User", }; // TODO: fetch actual roles
            var jwtToken = jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponse { AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto, };

            // Enhance with tenant data
            return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        /// <summary>
        /// Sign in using Google ID Token (for NextAuth.js integration)
        /// </summary>
        public async Task<SignInResponse> GoogleIdTokenSignInAsync(GoogleIdTokenRequestDto request)
        {
            try
            {
                // Validate that we have an ID token
                if (string.IsNullOrEmpty(request.IdToken)) { throw new ArgumentException("ID token is required"); }

                // Validate Google ID Token
                var googleUser = await oauthService.ValidateGoogleIdTokenAsync(request.IdToken);

                // Find or create user
                var user = await FindOrCreateOAuthUserAsync(googleUser.Email, googleUser.Name, "google", googleUser.Id);

                // Generate tokens
                var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email, };
                var roles = new[ ] { "User", }; // TODO: fetch actual roles
                var jwtToken = jwtTokenService.GenerateAccessToken(userDto, roles);
                var refreshToken = jwtTokenService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
                var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
                var accessTokenExpiryMinutes = int.Parse(configuration["Jwt:ExpiryInMinutes"] ?? "60");
                var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);

                var refreshTokenEntity = new RefreshToken { UserId = user.Id, Token = refreshToken, ExpiresAt = refreshTokenExpiresAt, IsRevoked = false, CreatedByIp = "0.0.0.0", };

                await refreshTokenRepository.CreateAsync(refreshTokenEntity);

                var response = new SignInResponse
                {
                    AccessToken = jwtToken, RefreshToken = refreshToken, ExpiresAt = refreshTokenExpiresAt, AccessTokenExpiresAt = accessTokenExpiresAt, RefreshTokenExpiresAt = refreshTokenExpiresAt, User = userDto,
                };

                // Enhance with tenant data
                var finalResponse = await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);

                return finalResponse;
            }
            catch (Exception ex) { throw new UnauthorizedAccessException($"Google ID token validation failed: {ex.Message}", ex); }
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

        private async Task<User> FindOrCreateOAuthUserAsync(string email, string name, string provider, string providerId)
        {
            var result = await FindOrCreateOAuthUserWithInfoAsync(email, name, provider, providerId);

            return result.User;
        }

        private async Task<(User User, bool IsNewUser)> FindOrCreateOAuthUserWithInfoAsync(string email, string name, string provider, string providerId)
        {
            // First try to find user by email
            var normalizedEmail = email.ToLowerInvariant();
            var user = await userRepository.GetByEmailAsync(normalizedEmail);

            var isNewUser = false;

            if (user == null)
            {
                // Generate unique username from name using slugify (same as CreateUserHandler)
                var baseUsername = name.ToSlugCase();
                // Note: Username uniqueness check simplified - implement proper search if needed
                var existingUser = await userRepository.GetByUsernameAsync(baseUsername);
                if (existingUser != null) baseUsername = $"{baseUsername}_{Guid.NewGuid().ToString()[..8]}";

                var uniqueUsername = baseUsername; // Use the baseUsername we already made unique

                // Create new user
                user = new User { Id = Guid.NewGuid(), Username = uniqueUsername, Email = email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, };

                await userRepository.AddAsync(user);
                isNewUser = true;
            }

            // Check if OAuth credential exists (using Type field to store provider info)
            var credential = user.Credentials?.FirstOrDefault(c => c.Type == $"oauth_{provider}");

            if (credential == null)
            {
                // Add OAuth credential - store provider info in Type and provider ID in Metadata
                var metadata = System.Text.Json.JsonSerializer.Serialize(new { ProviderId = providerId, Provider = provider, });

                credential = new Credential
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Type = $"oauth_{provider}",
                    Value = providerId, // Store provider ID in Value field
                    Metadata = metadata, // Store additional provider info as JSON
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                await credentialRepository.AddAsync(credential);
            }

            return (user, isNewUser);
        }

        private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) { throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken)); }

            var refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken, // This is required and must not be empty
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                IsRevoked = false, // Explicitly set IsRevoked to false
                CreatedByIp = "0.0.0.0", // TODO: get real IP address
            };

            await refreshTokenRepository.CreateAsync(refreshTokenEntity);
        }

        public async Task<Web3ChallengeResponse> GenerateWeb3ChallengeAsync(Web3ChallengeRequest request) { return await web3Service.GenerateChallengeAsync(request); }

        public async Task<SignInResponse> VerifyWeb3SignatureAsync(Web3AuthenticationVerificationRequest request)
        {
            // Verify the signature
            var isValid = await web3Service.VerifySignatureAsync(request);

            if (!isValid) { throw new UnauthorizedAccessException("Invalid Web3 signature"); }

            // Find or create user
            var user = await web3Service.FindOrCreateWeb3UserAsync(request.WalletAddress, request.ChainId ?? "1");

            // Generate tokens
            var userDto = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email, };
            var roles = new[ ] { "User", }; // TODO: fetch actual roles
            var jwtToken = jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponse { AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto, };

            // Enhance with tenant data
            return await tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        public async Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request) { return await emailVerificationService.SendEmailVerificationAsync(request.Email); }

        public async Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest) { return await emailVerificationService.VerifyEmailAsync(verificationRequest.Token); }

        public async Task<EmailOperationResponse> ForgotPasswordAsync(ForgotPasswordRequestDto request) { return await emailVerificationService.SendPasswordResetAsync(request.Email); }

        public async Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request) { return await emailVerificationService.ResetPasswordAsync(request.Token, request.NewPassword); }

        public async Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId)
        {
            try
            {
                var user = await userRepository.GetByIdWithCredentialsAsync(userId);

                if (user == null) { return new EmailOperationResponse { Success = false, Message = "User not found", }; }

                var passwordCredential = user.Credentials?.FirstOrDefault(c => c.Type == "password");

                if (passwordCredential == null) { return new EmailOperationResponse { Success = false, Message = "No password set for this account", }; }

                // Verify current password
                var hashedCurrentPassword = HashPassword(request.CurrentPassword);

                if (passwordCredential.Value != hashedCurrentPassword) { return new EmailOperationResponse { Success = false, Message = "Current password is incorrect", }; }

                // Update password
                passwordCredential.Value = HashPassword(request.NewPassword);
                passwordCredential.UpdatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await credentialRepository.UpdateAsync(passwordCredential);
                await credentialRepository.SaveChangesAsync();

                return new EmailOperationResponse { Success = true, Message = "Password changed successfully", };
            }
            catch (Exception) { return new EmailOperationResponse { Success = false, Message = "Failed to change password", }; }
        }

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
    }
}
