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
          .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

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

  public async Task<SignInResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request) {
    // Light logging only (avoid dumping all tokens in production)
    _logger.LogInformation("Processing refresh token (len={Len})", request.RefreshToken?.Length);

    if (string.IsNullOrWhiteSpace(request.RefreshToken))
      throw new UnauthorizedAccessException("Invalid refresh token");

    // Security enhancement: Get IP address for anomaly detection
    var ipAddress = GetClientIpAddress(_httpContextAccessor.HttpContext);

    // We make refresh rotation idempotent: if two parallel calls try to rotate the same
    // token, only the first will create a new token; the others will detect the existing
    // replacement and return it instead of failing / creating multiple chains.
    const int maxAttempts = 2; // initial try + one concurrency fallback

    for (var attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        // Load existing token inside loop (may change after concurrency failure)
        var existing = await _context.RefreshTokens
          .Where(rt => rt.Token == request.RefreshToken)
          .FirstOrDefaultAsync();

        if (existing == null) {
          _logger.LogWarning("Refresh token rejected (not found) from IP: {IpAddress}", ipAddress);

          // Security enhancement: Record anomaly for token not found
          await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
            Email = "unknown",
            UserId = null,
            IpAddress = ipAddress,
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
            IsSuccessful = false,
            FailureReason = "Invalid refresh token"
          });

          throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // If already rotated by another request: return replacement if still active
        if (existing.IsRevoked && existing.ReplacedByToken is not null) {
          var replacement = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == existing.ReplacedByToken);

          if (replacement != null && !replacement.IsRevoked && replacement.ExpiresAt > DateTime.UtcNow) {
            _logger.LogInformation("Refresh token already rotated by another request (attempt {Attempt})", attempt);

            var userAlready = await _context.Users.FindAsync(existing.UserId)
              ?? throw new UnauthorizedAccessException("User not found");
            var userDtoAlready = new UserDto {
              Id = userAlready.Id,
              Username = userAlready.Name,
              Email = userAlready.Email
            };
            var rolesAlready = new[] { "User" }; // TODO: actual roles

            var accessMinutesAlready = int.Parse(_configuration["Jwt:ExpirationMinutes"]
              ?? _configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var newAccessTokenAlready = _jwtTokenService.GenerateAccessToken(userDtoAlready, rolesAlready);
            var newAccessTokenExpiresAtAlready = DateTime.UtcNow.AddMinutes(accessMinutesAlready);

            var responseAlready = new SignInResponseDto {
              AccessToken = newAccessTokenAlready,
              RefreshToken = replacement.Token,
              ExpiresAt = replacement.ExpiresAt,
              AccessTokenExpiresAt = newAccessTokenExpiresAtAlready,
              RefreshTokenExpiresAt = replacement.ExpiresAt,
              User = userDtoAlready,
            };
            responseAlready = await _tenantAuthService.EnhanceWithTenantDataAsync(responseAlready, userAlready, request.TenantId);

            return responseAlready;
          }
        }

        if (existing.IsRevoked || existing.ExpiresAt <= DateTime.UtcNow) {
          _logger.LogWarning("Refresh token rejected (revoked / expired) from IP: {IpAddress}", ipAddress);

          // Security enhancement: Record anomaly for revoked/expired token
          await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
            Email = "unknown",
            UserId = existing.UserId,
            IpAddress = ipAddress,
            UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
            IsSuccessful = false,
            FailureReason = existing.IsRevoked ? "Token revoked" : "Token expired"
          });

          throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _context.Users.FindAsync(existing.UserId)
          ?? throw new UnauthorizedAccessException("User not found");

        var tenantId = request.TenantId; // optional override
        IEnumerable<Claim>? tenantClaims = null;

        if (tenantId.HasValue) {
          var permittedTenants = await _tenantAuthService.GetUserTenantsAsync(user);

          if (permittedTenants.Any(t => t.TenantId.HasValue && t.TenantId.Value == tenantId.Value)) {
            tenantClaims = await _tenantAuthService.GetTenantClaimsAsync(user, tenantId.Value);
          }
          else {
            tenantId = null; // ignore inaccessible tenant
          }
        }

        // Config
        var accessMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"]
          ?? _configuration["Jwt:ExpiryInMinutes"] ?? "60");
        var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]
          ?? _configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

        var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };
        var roles = new[] { "User" }; // TODO: actual roles

        var newAccessToken = _jwtTokenService.GenerateAccessToken(userDto, roles, tenantClaims);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newAccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);

        // Rotate (mark revoked)
        existing.IsRevoked = true;
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefreshTokenValue;

        // Persist new refresh token
        var newRefreshTokenEntity = new RefreshToken {
          UserId = user.Id,
          Token = newRefreshTokenValue,
          ExpiresAt = newRefreshTokenExpiresAt,
          CreatedByIp = ipAddress,
          IsRevoked = false
        };
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        // Maintenance
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var stale = await _context.RefreshTokens
          .Where(rt => rt.UserId == user.Id && rt.ExpiresAt < cutoff)
          .ToListAsync();
        if (stale.Count > 0)
          _context.RefreshTokens.RemoveRange(stale);

        await _context.SaveChangesAsync();

        // Security enhancement: Record successful token refresh
        await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
          Email = user.Email,
          UserId = user.Id,
          IpAddress = ipAddress,
          UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown",
          IsSuccessful = true,
          FailureReason = null
        });

        var signInResponse = new SignInResponseDto {
          AccessToken = newAccessToken,
          RefreshToken = newRefreshTokenValue,
          ExpiresAt = newRefreshTokenExpiresAt,
          AccessTokenExpiresAt = newAccessTokenExpiresAt,
          RefreshTokenExpiresAt = newRefreshTokenExpiresAt,
          User = userDto,
          TenantId = tenantId,
        };
        signInResponse = await _tenantAuthService.EnhanceWithTenantDataAsync(signInResponse, user, tenantId);

        return signInResponse;
      }
      catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts) {
        _logger.LogWarning(ex, "Concurrency conflict rotating refresh token (attempt {Attempt}) - retrying", attempt);
        // Clear tracked entities to avoid stale state before retry
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
          entry.State = EntityState.Detached;
        await Task.Delay(25); // small backoff

        continue; // retry loop
      }
    }

    // If we reach here, concurrency did not resolve
    _logger.LogError("Failed to rotate refresh token after {Attempts} attempts", maxAttempts);

    throw new UnauthorizedAccessException("Could not refresh token at this time");
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

  public async Task<Web3ChallengeResponseDto> GenerateWeb3ChallengeAsync(Web3ChallengeRequestDto request) {
    // Security enhancement: Get IP address for monitoring
    var ipAddress = GetClientIpAddress(_httpContextAccessor.HttpContext);
    var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown";

    _logger.LogInformation("Web3 challenge request from IP: {IpAddress}, UserAgent: {UserAgent}, Address: {WalletAddress}",
      ipAddress, userAgent, request.WalletAddress);

    try {
      // Delegate to the Web3 service for challenge generation
      var challengeResponse = await _web3Service.GenerateChallengeAsync(request);

      // Security enhancement: Record successful challenge generation
      await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
        Email = "web3-challenge",
        UserId = null,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        IsSuccessful = true,
        FailureReason = null
      });

      return challengeResponse;
    }
    catch (ArgumentException ex) {
      _logger.LogWarning("Invalid Web3 challenge request from IP: {IpAddress}, Error: {Error}", ipAddress, ex.Message);

      // Security enhancement: Record failed challenge generation for invalid addresses
      await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
        Email = "web3-challenge-invalid",
        UserId = null,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        IsSuccessful = false,
        FailureReason = ex.Message
      });

      throw; // Re-throw the original exception to maintain proper error handling
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error generating Web3 challenge from IP: {IpAddress}", ipAddress);

      // Security enhancement: Record unexpected errors
      await _anomalyService.RecordLoginAttemptAsync(new CreateLoginAttemptRequest {
        Email = "web3-challenge-error",
        UserId = null,
        IpAddress = ipAddress,
        UserAgent = userAgent,
        IsSuccessful = false,
        FailureReason = "Challenge generation error"
      });

      throw; // Re-throw the original exception
    }
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
