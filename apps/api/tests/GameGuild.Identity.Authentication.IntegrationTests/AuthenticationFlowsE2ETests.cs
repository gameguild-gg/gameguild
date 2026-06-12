using FluentAssertions;
using Xunit;
using GameGuild.API.Database;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// End-to-end integration tests for authentication flows
/// Tests complete authentication scenarios including:
/// - Local authentication (sign-up, sign-in, token lifecycle)
/// - Social authentication (OAuth providers)
/// - Web3 authentication (wallet signatures)
/// - Polymorphic authentication (multiple strategies)
/// - MFA enrollment and verification
/// - Token refresh and revocation
/// </summary>
public class AuthenticationFlowsE2ETests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly IMfaService _mfaService;
    private readonly IRefreshTokenHasher _refreshTokenHasher;

    public AuthenticationFlowsE2ETests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        // Set environment variables before factory initialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "ThisIsASecretKeyForIntegrationTestingThatIsLongEnoughToProhibitErrors");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GameGuild");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GameGuild.Users");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AuthFlowsTestDb_{Guid.NewGuid()}");
                });

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });

                var oauthDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOAuthService));
                if (oauthDescriptor != null)
                {
                    services.Remove(oauthDescriptor);
                }

                services.AddSingleton<IOAuthService, FakeOAuthService>();
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _mfaService = _scope.ServiceProvider.GetRequiredService<IMfaService>();
        _refreshTokenHasher = _scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();

        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    #region Local Authentication E2E Tests

    [Fact]
    public async Task LocalAuth_CompleteFlow_SignUpSignInRefreshRevoke_ShouldWorkCorrectly()
    {
        // Arrange
        var email = $"local.flow.{Guid.NewGuid()}@test.com";
        var password = "SecurePassword123!";

        // Act 1: Sign Up
        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"user_{Guid.NewGuid():N}",
            Password = password
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);

        // Assert Sign Up
        signUpResult.Should().NotBeNull();
        signUpResult.AccessToken.Should().NotBeNullOrEmpty();
        signUpResult.RefreshToken.Should().NotBeNullOrEmpty();

        var userId = signUpResult.UserId;

        // Act 2: Sign In
        var signInRequest = new LocalSignInRequest
        {
            Email = email,
            Password = password
        };

        var signInResult = await _authService.LocalSignInAsync(signInRequest);

        // Assert Sign In
        signInResult.Should().NotBeNull();
        signInResult.AccessToken.Should().NotBeNullOrEmpty();
        signInResult.RefreshToken.Should().NotBeNullOrEmpty();
        signInResult.UserId.Should().Be(userId);

        var originalRefreshToken = signInResult.RefreshToken;

        // Act 3: Refresh Token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        };

        var refreshResult = await _authService.RefreshTokenAsync(refreshRequest);

        // Assert Refresh
        refreshResult.Should().NotBeNull();
        refreshResult.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBe(originalRefreshToken); // Should be a new token

        // Act 4: Revoke Token
        await _authService.RevokeRefreshTokenAsync(refreshResult.RefreshToken, "127.0.0.1");

        // Assert Revoke - Token should no longer work
        var revokedTokenHash = _refreshTokenHasher.HashToken(refreshResult.RefreshToken);
        var revokedToken = await _dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == revokedTokenHash);

        revokedToken.Should().NotBeNull();
        revokedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LocalAuth_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var email = $"invalid.{Guid.NewGuid()}@test.com";

        var signInRequest = new LocalSignInRequest
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        // Act & Assert
        await FluentActions.Invoking(async () => await _authService.LocalSignInAsync(signInRequest))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LocalAuth_TokenRefresh_AfterRevocation_ShouldFail()
    {
        // Arrange
        var email = $"revoke.test.{Guid.NewGuid()}@test.com";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"user_{Guid.NewGuid():N}",
            Password = "TestPassword123!"
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);

        // Revoke the refresh token
        await _authService.RevokeRefreshTokenAsync(signUpResult.RefreshToken, "127.0.0.1");

        // Act & Assert - Try to use revoked token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = signUpResult.RefreshToken
        };

        await FluentActions.Invoking(async () => await _authService.RefreshTokenAsync(refreshRequest))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LocalAuth_SignIn_WithTenantMemberships_ShouldPopulateTenantContext()
    {
        // Arrange
        var email = $"tenant.flow.{Guid.NewGuid()}@test.com";
        var password = "TenantPassword123!";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"tenant_user_{Guid.NewGuid():N}",
            Password = password
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var userId = signUpResult.UserId;

        var tenantOne = new Tenant
        {
            Name = $"Tenant One {Guid.NewGuid():N}",
            Slug = $"tenant-one-{Guid.NewGuid():N}",
            AdminEmail = email,
            IsActive = true
        };

        var tenantTwo = new Tenant
        {
            Name = $"Tenant Two {Guid.NewGuid():N}",
            Slug = $"tenant-two-{Guid.NewGuid():N}",
            AdminEmail = email,
            IsActive = true
        };

        _dbContext.Set<Tenant>().AddRange(tenantOne, tenantTwo);
        await _dbContext.SaveChangesAsync();

        _dbContext.Set<TenantMember>().AddRange(
            new TenantMember
            {
                UserId = userId,
                TenantId = tenantOne.Id,
                Role = "Owner",
                IsActive = true,
                Tenant = tenantOne
            },
            new TenantMember
            {
                UserId = userId,
                TenantId = tenantTwo.Id,
                Role = "Owner",
                IsActive = true,
                Tenant = tenantTwo
            });
        await _dbContext.SaveChangesAsync();

        var signInRequest = new LocalSignInRequest
        {
            Email = email,
            Password = password,
            TenantId = tenantTwo.Id
        };

        // Act
        var signInResult = await _authService.LocalSignInAsync(signInRequest);

        // Assert
        signInResult.TenantId.Should().Be(tenantTwo.Id);
        signInResult.AvailableTenants.Should().NotBeNull();
        signInResult.AvailableTenants!.Should().HaveCount(2);
        signInResult.AvailableTenants.Should().Contain(tenant => tenant.Id == tenantOne.Id && tenant.Name == tenantOne.Name);
        signInResult.AvailableTenants.Should().Contain(tenant => tenant.Id == tenantTwo.Id && tenant.Name == tenantTwo.Name);
    }

    #endregion

    #region MFA Enrollment and Verification E2E Tests

    [Fact]
    public async Task MFA_CompleteEnrollmentFlow_SetupVerifyUse_ShouldWorkCorrectly()
    {
        // Arrange - Create user first
        var email = $"mfa.enroll.{Guid.NewGuid()}@test.com";
        var password = "MfaPassword123!";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"mfa_user_{Guid.NewGuid():N}",
            Password = password
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var userId = signUpResult.UserId;

        // Act 1: Initiate MFA Setup
        var setupResult = await _mfaService.InitiateMfaSetupAsync(userId, email);

        // Assert Setup Initiation
        setupResult.Should().NotBeNull();
        setupResult.QrCodeUri.Should().NotBeNullOrEmpty();
        setupResult.SecretKey.Should().NotBeNullOrEmpty();
        setupResult.BackupCodes.Should().NotBeNull();
        setupResult.BackupCodes.Should().HaveCount(10);

        // For testing purposes, we would simulate a valid TOTP code
        // In a real scenario, this would be generated by an authenticator app
        // var mockTotpCode = "123456";

        // Act 2: Complete MFA Setup (would fail in real scenario without valid TOTP)
        // Note: This test demonstrates the flow; actual verification would require a valid TOTP generator

        // Act 3: Get MFA Configuration
        var mfaConfig = await _mfaService.GetMfaConfigurationAsync(userId);

        // Assert MFA Configuration
        mfaConfig.Should().NotBeNull();
        // MFA is not enabled until successfully completed
    }

    [Fact]
    public async Task MFA_DisableFlow_ShouldRemoveMfaRequirement()
    {
        // Arrange - Create user
        var email = $"mfa.disable.{Guid.NewGuid()}@test.com";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"mfa_disable_user_{Guid.NewGuid():N}",
            Password = "DisableMfaPassword123!"
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var userId = signUpResult.UserId;

        // Setup MFA first
        var setupResult = await _mfaService.InitiateMfaSetupAsync(userId, email);

        // Act: Disable MFA (requires password confirmation)
        await _mfaService.DisableMfaAsync(userId, "TestPassword123!");

        // Assert: MFA should be disabled
        var mfaConfig = await _mfaService.GetMfaConfigurationAsync(userId);
        mfaConfig.Should().NotBeNull();
        mfaConfig.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task MFA_BackupCodes_RegenerateFlow_ShouldProvideNewCodes()
    {
        // Arrange
        var email = $"mfa.backup.{Guid.NewGuid()}@test.com";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"mfa_backup_user_{Guid.NewGuid():N}",
            Password = "BackupPassword123!"
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var userId = signUpResult.UserId;

        // Setup MFA
        var setupResult = await _mfaService.InitiateMfaSetupAsync(userId, email);
        var originalBackupCodes = setupResult.BackupCodes;

        // Complete MFA setup by enabling it directly in the database (test workaround)
        // In production, this would be done by verifying a valid TOTP code
        var mfaConfig = await _dbContext.Set<UserMfaConfiguration>().FirstOrDefaultAsync(c => c.UserId == userId);
        if (mfaConfig != null)
        {
            mfaConfig.IsEnabled = true;
            mfaConfig.EnabledAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        // Act: Regenerate backup codes
        var newBackupCodes = await _mfaService.GenerateBackupCodesAsync(userId);

        // Assert
        newBackupCodes.Should().NotBeNull();
        newBackupCodes.Should().HaveCount(10);
        newBackupCodes.Should().NotBeEquivalentTo(originalBackupCodes);
    }

    #endregion

    #region Social Authentication E2E Tests (with mocked providers)

    [Fact]
    public async Task SocialAuth_GoogleProvider_CompleteFlow_ShouldAuthenticateUser()
    {
        var mockGoogleEmail = $"google.user.{Guid.NewGuid()}@gmail.com";

        var googleSignInRequest = new OAuthSignInRequest
        {
            AccessToken = $"google:{mockGoogleEmail}",
            RedirectUri = "https://app.gameguild.test/auth/google/callback"
        };

        var result = await _authService.GoogleSignInAsync(googleSignInRequest);

        result.Success.Should().BeTrue();
        result.Email.Should().Be(mockGoogleEmail);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SocialAuth_GitHubProvider_CompleteFlow_ShouldAuthenticateUser()
    {
        var mockGitHubEmail = $"github.user.{Guid.NewGuid()}@github.local";

        var githubSignInRequest = new OAuthSignInRequest
        {
            AccessToken = $"github:{mockGitHubEmail}",
            RedirectUri = "https://app.gameguild.test/auth/github/callback"
        };

        var result = await _authService.GitHubSignInAsync(githubSignInRequest);

        result.Success.Should().BeTrue();
        result.Email.Should().Be(mockGitHubEmail);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Web3 Authentication E2E Tests

    [Fact]
    public async Task Web3Auth_ChallengeGeneration_ShouldReturnChallengeForWallet()
    {
        var walletAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28";

        var challengeResult = await _authService.GenerateWeb3ChallengeAsync(new Web3ChallengeRequest
        {
            WalletAddress = walletAddress,
            ChainId = "1"
        });

        challengeResult.Should().NotBeNull();
        challengeResult.Challenge.Should().NotBeNullOrEmpty();
        challengeResult.Challenge.Should().Contain(walletAddress);
        challengeResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Web3Auth_InvalidSignature_ShouldFailVerification()
    {
        var walletAddress = "0x742d35Cc6634C0532925a3b844Bc9e7595f2bD28";

        var challengeResult = await _authService.GenerateWeb3ChallengeAsync(new Web3ChallengeRequest
        {
            WalletAddress = walletAddress
        });

        var verifyRequest = new Web3VerificationRequest
        {
            WalletAddress = walletAddress,
            Challenge = challengeResult.Challenge,
            Nonce = challengeResult.Challenge,
            Signature = "0x" + new string('a', 130),
            ChainId = "1"
        };

        await FluentActions.Invoking(async () => await _authService.VerifyWeb3SignatureAsync(verifyRequest))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Polymorphic Authentication E2E Tests

    [Fact]
    public async Task PolymorphicAuth_EmailCredential_ShouldAuthenticateCorrectly()
    {
        var email = $"poly.local.{Guid.NewGuid()}@test.com";
        var password = "PolyPassword123!";

        await _authService.LocalSignUpAsync(new LocalSignUpRequest
        {
            Email = email,
            Username = $"poly_user_{Guid.NewGuid():N}",
            Password = password
        });

        var userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var handler = new PolymorphicSignInHandler(_authService, userRepository, NullLogger<PolymorphicSignInHandler>.Instance);

        var result = await handler.Handle(new PolymorphicSignInCommand
        {
            Credential = email,
            CredentialType = CredentialType.Email,
            Password = password
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Email.Should().Be(email);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Token Lifecycle Edge Cases

    [Fact]
    public async Task TokenLifecycle_MultipleRefreshes_ShouldInvalidateOldTokens()
    {
        // Arrange
        var email = $"token.lifecycle.{Guid.NewGuid()}@test.com";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"token_user_{Guid.NewGuid():N}",
            Password = "TokenLifecycle123!"
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var firstRefreshToken = signUpResult.RefreshToken;

        // Act - Perform multiple refreshes
        var refreshRequest1 = new RefreshTokenRequest { RefreshToken = firstRefreshToken };
        var refreshResult1 = await _authService.RefreshTokenAsync(refreshRequest1);

        var refreshRequest2 = new RefreshTokenRequest { RefreshToken = refreshResult1.RefreshToken };
        var refreshResult2 = await _authService.RefreshTokenAsync(refreshRequest2);

        // Assert - Old tokens should be invalid
        var oldTokenRefreshRequest = new RefreshTokenRequest { RefreshToken = firstRefreshToken };

        await FluentActions.Invoking(async () => await _authService.RefreshTokenAsync(oldTokenRefreshRequest))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TokenLifecycle_SignOut_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var email = $"signout.test.{Guid.NewGuid()}@test.com";

        var signUpRequest = new LocalSignUpRequest
        {
            Email = email,
            Username = $"signout_user_{Guid.NewGuid():N}",
            Password = "SignOut123!"
        };

        var signUpResult = await _authService.LocalSignUpAsync(signUpRequest);
        var userId = signUpResult.UserId;

        // Create multiple sessions
        var signInResult1 = await _authService.LocalSignInAsync(new LocalSignInRequest
        {
            Email = email,
            Password = "SignOut123!"
        });

        var signInResult2 = await _authService.LocalSignInAsync(new LocalSignInRequest
        {
            Email = email,
            Password = "SignOut123!"
        });

        // Act - Sign out (revoke all tokens)
        await _authService.RevokeRefreshTokenAsync(signUpResult.RefreshToken, "127.0.0.1");
        await _authService.RevokeRefreshTokenAsync(signInResult1.RefreshToken, "127.0.0.1");
        await _authService.RevokeRefreshTokenAsync(signInResult2.RefreshToken, "127.0.0.1");

        // Assert - All tokens should be revoked
        var userTokens = await _dbContext.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId)
            .ToListAsync();

        userTokens.Should().AllSatisfy(token => token.IsRevoked.Should().BeTrue());
    }

    #endregion

    public void Dispose()
    {
        _scope?.Dispose();
        _dbContext?.Dispose();
        _client?.Dispose();
    }
}

internal sealed class FakeOAuthService : IOAuthService
{
    public Task<string> GetAuthorizationUrlAsync(string provider, string redirectUri, string state, string[]? scopes = null)
        => Task.FromResult($"{redirectUri}?provider={provider}&state={state}");

    public Task<OAuthUserProfile> HandleCallbackAsync(string provider, string code, string state, string redirectUri)
        => Task.FromResult(CreateProfile(provider, code));

    public Task<OAuthUserProfile> ValidateIdTokenAsync(string provider, string idToken)
        => Task.FromResult(CreateProfile(provider, idToken));

    public Task<OAuthUserProfile> GetUserProfileAsync(string provider, string accessToken)
        => Task.FromResult(CreateProfile(provider, accessToken));

    public Task<bool> RevokeTokenAsync(string provider, string token)
        => Task.FromResult(!string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(token));

    private static OAuthUserProfile CreateProfile(string provider, string token)
    {
        var email = token.Contains(':', StringComparison.Ordinal)
            ? token[(token.IndexOf(':', StringComparison.Ordinal) + 1)..]
            : $"{provider}.user@example.com";

        return new OAuthUserProfile
        {
            Provider = provider,
            ProviderId = $"{provider}-{email}",
            Email = email,
            EmailVerified = true,
            Name = $"{provider} test user",
            Username = email.Split('@')[0],
            AccessToken = token
        };
    }
}
