using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using GameGuild.Data;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Models;
using GameGuild.Modules.User.Models;
using GameGuild.Modules.Tenant.Services;
using UserModel = GameGuild.Modules.User.Models.User;

namespace GameGuild.Modules.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        private readonly IJwtTokenService _jwtTokenService;

        private readonly IOAuthService _oauthService;

        private readonly IConfiguration _configuration;

        private readonly IWeb3Service _web3Service;

        private readonly IEmailVerificationService _emailVerificationService;

        private readonly ITenantAuthService _tenantAuthService;

        private readonly ITenantService _tenantService;

        public AuthService(
            ApplicationDbContext context,
            IJwtTokenService jwtTokenService,
            IOAuthService oauthService,
            IConfiguration configuration,
            IWeb3Service web3Service,
            IEmailVerificationService emailVerificationService,
            ITenantAuthService tenantAuthService,
            ITenantService tenantService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _oauthService = oauthService;
            _configuration = configuration;
            _web3Service = web3Service;
            _emailVerificationService = emailVerificationService;
            _tenantAuthService = tenantAuthService;
            _tenantService = tenantService;
        }

        public async Task<SignInResponseDto> LocalSignInAsync(LocalSignInRequestDto request)
        {
            UserModel? user = await _context.Users.Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials");

            Credential? passwordCredential = user.Credentials.FirstOrDefault(c => c.Type == "password" && c.IsActive);

            if (passwordCredential == null || !VerifyPassword(request.Password, passwordCredential.Value))
                throw new UnauthorizedAccessException("Invalid credentials");

            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles if available

            string accessToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            string refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponseDto
            {
                AccessToken = accessToken, RefreshToken = refreshToken, User = userDto
            };

            // Enhance response with tenant data
            // Note: We need to cast the user to the expected type for TenantAuthService
            var requestedTenantId = request.TenantId;

            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, (GameGuild.Modules.User.Models.User)user, requestedTenantId);
        }

        public async Task<SignInResponseDto> LocalSignUpAsync(LocalSignUpRequestDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("User already exists");

            var user = new User.Models.User
            {
                Name = request.Username ?? request.Email, Email = request.Email, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var credential = new Credential
            {
                UserId = user.Id,
                Type = "password",
                Value = HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Credentials.Add(credential);
            await _context.SaveChangesAsync();

            // If a tenantId was provided and it's valid, ensure user has access to that tenant
            if (request.TenantId.HasValue)
            {
                try
                {
                    // Try to add user to the specified tenant
                    await _tenantService.AddUserToTenantAsync(user.Id, request.TenantId.Value);
                }
                catch (Exception ex)
                {
                    // Log but continue - tenant association failed but user is still created
                    Console.WriteLine($"Failed to add user to tenant: {ex.Message}");
                }
            }

            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles if available

            string accessToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            string refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponseDto
            {
                AccessToken = accessToken, RefreshToken = refreshToken, User = userDto
            };

            // Enhance response with tenant data
            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        private static string HashPassword(string password)
        {
            // Simple SHA256 hash for demonstration (replace with a secure hash in production)
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            RefreshToken? refreshToken = await _context.RefreshTokens
                .Where(rt => rt.Token == request.RefreshToken)
                .Where(rt => !rt.IsRevoked)
                .Where(rt => rt.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (refreshToken == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            User.Models.User? user = await _context.Users.FindAsync(refreshToken.UserId);

            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Get the tenantId from the request or from the claims
            var tenantId = request.TenantId;

            // Generate new tokens
            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles if available

            // Get tenant claims if a tenantId was provided
            IEnumerable<Claim>? tenantClaims = null;
            if (tenantId.HasValue)
            {
                // Verify the user has access to this tenant
                if (await _tenantAuthService.GetUserTenantsAsync(user) is var tenants &&
                    tenants.Any(t => t.TenantId.HasValue && t.TenantId.Value == tenantId.Value))
                {
                    tenantClaims = await _tenantAuthService.GetTenantClaimsAsync(user, tenantId.Value);
                }
            }

            // Generate token with tenant claims if available
            string newAccessToken = _jwtTokenService.GenerateAccessToken(userDto, roles, tenantClaims);
            string newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Revoke old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByToken = newRefreshToken;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id, Token = newRefreshToken, ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedByIp = "0.0.0.0" // TODO: get real IP address
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            var response = new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken, RefreshToken = newRefreshToken, ExpiresAt = newRefreshTokenEntity.ExpiresAt, TenantId = tenantId
            };

            return response;
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress)
        {
            RefreshToken? refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || !refreshToken.IsActive)
                throw new ArgumentException("Invalid token");

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
        }

        public async Task<SignInResponseDto> GitHubSignInAsync(OAuthSignInRequestDto request)
        {
            // Exchange code for access token
            string accessToken = await _oauthService.ExchangeGitHubCodeAsync(request.Code, request.RedirectUri);

            // Get user info from GitHub
            GitHubUserDto githubUser = await _oauthService.GetGitHubUserAsync(accessToken);

            // Find or create user
            User.Models.User user = await FindOrCreateOAuthUserAsync(githubUser.Email, githubUser.Name, "github", githubUser.Id.ToString());

            // Generate tokens
            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles
            string jwtToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            string refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponseDto
            {
                AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto
            };

            // Enhance with tenant data
            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        public async Task<SignInResponseDto> GoogleSignInAsync(OAuthSignInRequestDto request)
        {
            // Exchange code for access token
            string accessToken = await _oauthService.ExchangeGoogleCodeAsync(request.Code, request.RedirectUri);

            // Get user info from Google
            GoogleUserDto googleUser = await _oauthService.GetGoogleUserAsync(accessToken);

            // Find or create user
            User.Models.User user = await FindOrCreateOAuthUserAsync(googleUser.Email, googleUser.Name, "google", googleUser.Id);

            // Generate tokens
            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles
            string jwtToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            string refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponseDto
            {
                AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto
            };

            // Enhance with tenant data
            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        public Task<string> GetGitHubAuthUrlAsync(string redirectUri)
        {
            string? clientId = _configuration["OAuth:GitHub:ClientId"];
            var scopes = "user:email";
            var state = Guid.NewGuid().ToString(); // In production, store this for validation

            var url = $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scopes}&state={state}";

            return Task.FromResult(url);
        }

        public Task<string> GetGoogleAuthUrlAsync(string redirectUri)
        {
            string? clientId = _configuration["OAuth:Google:ClientId"];
            var scopes = "openid email profile";
            var state = Guid.NewGuid().ToString(); // In production, store this for validation

            var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopes)}&response_type=code&state={state}";

            return Task.FromResult(url);
        }

        private async Task<User.Models.User> FindOrCreateOAuthUserAsync(string email, string name, string provider, string providerId)
        {
            // First try to find user by email
            User.Models.User? user = await _context.Users.Include(u => u.Credentials)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Create new user
                user = new User.Models.User
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }

            // Check if OAuth credential exists (using Type field to store provider info)
            Credential? credential = user.Credentials?.FirstOrDefault(c => c.Type == $"oauth_{provider}");
            if (credential == null)
            {
                // Add OAuth credential - store provider info in Type and provider ID in Metadata
                string metadata = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        ProviderId = providerId, Provider = provider
                    }
                );
                credential = new Credential
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Type = $"oauth_{provider}",
                    Value = providerId, // Store provider ID in Value field
                    Metadata = metadata, // Store additional provider info as JSON
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Credentials.Add(credential);
            }

            await _context.SaveChangesAsync();

            return user;
        }

        private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));
            }

            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken, // This is required and must not be empty
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false, // Explicitly set IsRevoked to false
                CreatedByIp = "0.0.0.0" // TODO: get real IP address
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
        }

        public async Task<Web3ChallengeResponseDto> GenerateWeb3ChallengeAsync(Web3ChallengeRequestDto request)
        {
            return await _web3Service.GenerateChallengeAsync(request);
        }

        public async Task<SignInResponseDto> VerifyWeb3SignatureAsync(Web3VerifyRequestDto request)
        {
            // Verify the signature
            bool isValid = await _web3Service.VerifySignatureAsync(request);
            if (!isValid)
            {
                throw new UnauthorizedAccessException("Invalid Web3 signature");
            }

            // Find or create user
            User.Models.User user = await _web3Service.FindOrCreateWeb3UserAsync(request.WalletAddress, request.ChainId ?? "1");

            // Generate tokens
            var userDto = new UserDto
            {
                Id = user.Id, Username = user.Name, Email = user.Email
            };
            var roles = new[]
            {
                "User"
            }; // TODO: fetch actual roles
            string jwtToken = _jwtTokenService.GenerateAccessToken(userDto, roles);
            string refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Save refresh token
            await SaveRefreshTokenAsync(user.Id, refreshToken);

            // Create initial response
            var response = new SignInResponseDto
            {
                AccessToken = jwtToken, RefreshToken = refreshToken, User = userDto
            };

            // Enhance with tenant data
            return await _tenantAuthService.EnhanceWithTenantDataAsync(response, user, request.TenantId);
        }

        public async Task<EmailOperationResponseDto> SendEmailVerificationAsync(SendEmailVerificationRequestDto request)
        {
            return await _emailVerificationService.SendEmailVerificationAsync(request.Email);
        }

        public async Task<EmailOperationResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            return await _emailVerificationService.VerifyEmailAsync(request.Token);
        }

        public async Task<EmailOperationResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            return await _emailVerificationService.SendPasswordResetAsync(request.Email);
        }

        public async Task<EmailOperationResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            return await _emailVerificationService.ResetPasswordAsync(request.Token, request.NewPassword);
        }

        public async Task<EmailOperationResponseDto> ChangePasswordAsync(ChangePasswordRequestDto request, Guid userId)
        {
            try
            {
                User.Models.User? user = await _context.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return new EmailOperationResponseDto
                    {
                        Success = false, Message = "User not found"
                    };
                }

                Credential? passwordCredential = user.Credentials?.FirstOrDefault(c => c.Type == "password");
                if (passwordCredential == null)
                {
                    return new EmailOperationResponseDto
                    {
                        Success = false, Message = "No password set for this account"
                    };
                }

                // Verify current password
                string hashedCurrentPassword = HashPassword(request.CurrentPassword);
                if (passwordCredential.Value != hashedCurrentPassword)
                {
                    return new EmailOperationResponseDto
                    {
                        Success = false, Message = "Current password is incorrect"
                    };
                }

                // Update password
                passwordCredential.Value = HashPassword(request.NewPassword);
                passwordCredential.UpdatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new EmailOperationResponseDto
                {
                    Success = true, Message = "Password changed successfully"
                };
            }
            catch (Exception)
            {
                return new EmailOperationResponseDto
                {
                    Success = false, Message = "Failed to change password"
                };
            }
        }
    }
}
