using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Database;
using GameGuild.Modules.Authentication.Models;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Modules.Authentication.Services;

/// <summary>
/// Enhanced authentication service with anomaly detection and user enumeration protection
/// </summary>
public class EnhancedAuthService : IAuthService {
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOAuthService _oauthService;
    private readonly IConfiguration _configuration;
    private readonly IWeb3Service _web3Service;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ITenantAuthService _tenantAuthService;
    private readonly ITenantService _tenantService;
    private readonly IAuthenticationAnomalyService _anomalyService;
    private readonly IUserEnumerationProtectionService _enumerationProtection;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EnhancedAuthService> _logger;

    public EnhancedAuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IOAuthService oauthService,
        IConfiguration configuration,
        IWeb3Service web3Service,
        IEmailVerificationService emailVerificationService,
        ITenantAuthService tenantAuthService,
        ITenantService tenantService,
        IAuthenticationAnomalyService anomalyService,
        IUserEnumerationProtectionService enumerationProtection,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EnhancedAuthService> logger) {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _oauthService = oauthService;
        _configuration = configuration;
        _web3Service = web3Service;
        _emailVerificationService = emailVerificationService;
        _tenantAuthService = tenantAuthService;
        _tenantService = tenantService;
        _anomalyService = anomalyService;
        _enumerationProtection = enumerationProtection;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<SignInResponseDto> LocalSignInAsync(LocalSignInRequestDto request) {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        User? user = null;
        bool userExists = false;
        bool authenticationSucceeded = false;
        string? failureReason = null;

        try {
            // Check for throttling first
            var throttleDecision = await _anomalyService.ShouldThrottleAsync(ipAddress, request.Email);
            if (throttleDecision.ShouldThrottle) {
                await RecordFailedAttempt(request.Email, null, ipAddress, userAgent,
                    LoginFailureReasons.RateLimited, stopwatch.Elapsed, correlationId, request.TenantId);

                throw new UnauthorizedAccessException(_enumerationProtection.GetConsistentErrorMessage());
            }

            // Lookup user
            var normalizedEmail = request.Email.ToLowerInvariant();
            user = await _context.Users
                .Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.EmailAddress != null && u.EmailAddress.Value == normalizedEmail);

            userExists = user != null;

            // Perform authentication
            if (userExists) {
                var passwordCredential = user!.Credentials.FirstOrDefault(c => c is { Type: "password", IsActive: true });

                if (passwordCredential != null && VerifyPassword(request.Password, passwordCredential.Value)) {
                    authenticationSucceeded = true;
                }
                else {
                    failureReason = LoginFailureReasons.InvalidCredentials;
                }
            }
            else {
                failureReason = LoginFailureReasons.InvalidCredentials;
                // Perform dummy password hashing to maintain consistent timing
                await _enumerationProtection.PerformDummyPasswordHashAsync(request.Password);
            }

            // Apply user enumeration protection timing
            await _enumerationProtection.SimulateAuthenticationDelayAsync(request.Email, userExists);

            if (!authenticationSucceeded) {
                await RecordFailedAttempt(request.Email, user?.Id, ipAddress, userAgent,
                    failureReason!, stopwatch.Elapsed, correlationId, request.TenantId);

                throw new UnauthorizedAccessException(_enumerationProtection.GetConsistentErrorMessage());
            }

            // Analyze user login patterns for additional security
            if (user != null) {
                var userAnalysis = await _anomalyService.AnalyzeUserLoginPatternsAsync(user.Id, ipAddress, userAgent);

                // Log suspicious patterns but don't block (could be legitimate new device/location)
                if (userAnalysis.IsNewLocation || userAnalysis.IsNewDevice) {
                    _logger.LogInformation(
                        "User login from new location/device: UserId={UserId}, NewLocation={NewLocation}, NewDevice={NewDevice}",
                        user.Id, userAnalysis.IsNewLocation, userAnalysis.IsNewDevice);
                }
            }

            // Create tokens and response
            var userDto = new UserDto { Id = user!.Id, Username = user.Name, Email = user.Email };
            var roles = new[] { "User" }; // TODO: fetch actual roles if available

            var accessToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Expiries
            var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);
            var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            var refreshTokenEntity = new RefreshToken {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                IsRevoked = false,
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            // Record successful login attempt
            await RecordSuccessfulAttempt(request.Email, user.Id, ipAddress, userAgent,
                stopwatch.Elapsed, correlationId, request.TenantId);

            var response = new SignInResponseDto {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = userDto
            };

            // Enhance response with tenant data
            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);

        }
        catch (UnauthorizedAccessException) {
            // Re-throw authentication failures as-is
            throw;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Unexpected error during authentication for {Email}", request.Email);

            await RecordFailedAttempt(request.Email, user?.Id, ipAddress, userAgent,
                "SystemError", stopwatch.Elapsed, correlationId, request.TenantId);

            throw new UnauthorizedAccessException(_enumerationProtection.GetConsistentErrorMessage());
        }
    }

    public async Task<SignInResponseDto> LocalSignUpAsync(LocalSignUpRequestDto request) {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        try {
            // Check for existing user
            if (await _context.Users.AnyAsync(u => u.Email == request.Email)) {
                // Apply consistent timing even for existing users
                await _enumerationProtection.SimulateAuthenticationDelayAsync(request.Email, true);
                throw new InvalidOperationException("User already exists");
            }

            // Create new user
            var user = new User {
                Name = request.Username ?? request.Email,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var credential = new Credential {
                UserId = user.Id,
                Type = "password",
                Value = HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Credentials.Add(credential);
            await _context.SaveChangesAsync();

            // Handle tenant association
            if (request.TenantId.HasValue) {
                try {
                    await _tenantService.AddUserToTenantAsync(user.Id, request.TenantId.Value);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to add user {UserId} to tenant {TenantId}", user.Id, request.TenantId);
                }
            }

            // Create tokens
            var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };
            var roles = new[] { "User" };

            var accessToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);
            var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

            var refreshTokenEntity = new RefreshToken {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                IsRevoked = false,
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            // Record successful registration as login attempt
            await RecordSuccessfulAttempt(request.Email, user.Id, ipAddress, userAgent,
                stopwatch.Elapsed, correlationId, request.TenantId);

            // Log user creation audit
            await _auditService.LogAsync(new CreateAuditLogRequest {
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
            });

            var response = new SignInResponseDto {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = userDto
            };

            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);

        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            throw;
        }
    }

    // Implement other IAuthService methods by delegating to original AuthService or implementing with security enhancements
    public Task<SignInResponseDto> GoogleSignInAsync(GoogleSignInRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced Google sign-in not yet implemented");
    }

    public Task<SignInResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request) {
        // TODO: Implement with security enhancements  
        throw new NotImplementedException("Enhanced refresh token not yet implemented");
    }

    public Task RevokeTokenAsync(RevokeTokenRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced token revocation not yet implemented");
    }

    public Task<string> RequestPasswordResetAsync(PasswordResetRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced password reset not yet implemented");
    }

    public Task<EmailOperationResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced password reset confirmation not yet implemented");
    }

    public Task<Web3ChallengeResponseDto> GenerateWeb3ChallengeAsync(Web3ChallengeRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced Web3 challenge not yet implemented");
    }

    public Task<SignInResponseDto> Web3SignInAsync(Web3SignInRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced Web3 sign-in not yet implemented");
    }

    public Task<string> SendVerificationEmailAsync(SendVerificationEmailRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced email verification not yet implemented");
    }

    public Task<EmailOperationResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced email verification confirmation not yet implemented");
    }

    public Task<string> GetGitHubSignInUrlAsync(string redirectUri) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced GitHub sign-in not yet implemented");
    }

    public Task<SignInResponseDto> GitHubCallbackAsync(GitHubCallbackRequestDto request) {
        // TODO: Implement with security enhancements
        throw new NotImplementedException("Enhanced GitHub callback not yet implemented");
    }

    private async Task RecordSuccessfulAttempt(string email, Guid userId, string ipAddress, string? userAgent,
        TimeSpan processingTime, string? correlationId, Guid? tenantId) {
        var deviceFingerprint = _anomalyService.GenerateDeviceFingerprint(userAgent);

        await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
            Email = email,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccessful = true,
            ProcessingTime = processingTime,
            DeviceFingerprint = deviceFingerprint,
            TenantId = tenantId,
            CorrelationId = correlationId
        });

        await _auditService.LogAsync(new CreateAuditLogRequest {
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
        });
    }

    private async Task RecordFailedAttempt(string email, Guid? userId, string ipAddress, string? userAgent,
        string failureReason, TimeSpan processingTime, string? correlationId, Guid? tenantId) {
        var deviceFingerprint = _anomalyService.GenerateDeviceFingerprint(userAgent);

        await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
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
        });

        await _auditService.LogAsync(new CreateAuditLogRequest {
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
        });
    }

    private string GetClientIpAddress(HttpContext? context) {
        if (context == null) return "0.0.0.0";

        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress) || "unknown".Equals(ipAddress, StringComparison.OrdinalIgnoreCase)) {
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress) || "unknown".Equals(ipAddress, StringComparison.OrdinalIgnoreCase)) {
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        return ipAddress ?? "0.0.0.0";
    }

    private static string HashPassword(string password) {
        // Use BCrypt for proper password hashing (replace the simple SHA256)
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPassword(string password, string hash) {
        try {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch {
            // Fallback to old SHA256 method for existing passwords
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sha256Hash = Convert.ToBase64String(bytes);
            return sha256Hash == hash;
        }
    }

    // Missing interface method implementations (stubs)
    public Task RevokeRefreshTokenAsync(string refreshToken, string reason) {
        throw new NotImplementedException("RevokeRefreshTokenAsync not yet implemented");
    }

    public Task<SignInResponseDto> GitHubSignInAsync(OAuthSignInRequestDto request) {
        throw new NotImplementedException("GitHubSignInAsync not yet implemented");
    }

    public Task<SignInResponseDto> GoogleSignInAsync(OAuthSignInRequestDto request) {
        throw new NotImplementedException("GoogleSignInAsync not yet implemented");
    }

    public Task<SignInResponseDto> GoogleIdTokenSignInAsync(GoogleIdTokenRequestDto request) {
        throw new NotImplementedException("GoogleIdTokenSignInAsync not yet implemented");
    }

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri) {
        throw new NotImplementedException("GetGitHubAuthUrlAsync not yet implemented");
    }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri) {
        throw new NotImplementedException("GetGoogleAuthUrlAsync not yet implemented");
    }

    public Task<SignInResponseDto> VerifyWeb3SignatureAsync(Web3VerifyRequestDto request) {
        throw new NotImplementedException("VerifyWeb3SignatureAsync not yet implemented");
    }

    public Task<EmailOperationResponseDto> SendEmailVerificationAsync(SendEmailVerificationRequestDto request) {
        throw new NotImplementedException("SendEmailVerificationAsync not yet implemented");
    }

    public Task<EmailOperationResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request) {
        throw new NotImplementedException("ForgotPasswordAsync not yet implemented");
    }

    public Task<EmailOperationResponseDto> ChangePasswordAsync(ChangePasswordRequestDto request, Guid userId) {
        throw new NotImplementedException("ChangePasswordAsync not yet implemented");
    }

}