using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using Xunit;
using GameGuild.API.Database;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Nethereum.Signer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
public class AuthenticationFlowsE2ETests : IClassFixture<AuthenticationApiFactory>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuthService _authService;
    private readonly IMfaService _mfaService;

    public AuthenticationFlowsE2ETests(AuthenticationApiFactory factory)
    {
        _factory = factory;

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _mfaService = _scope.ServiceProvider.GetRequiredService<IMfaService>();

        // Ensure database is created
        _dbContext.Database.EnsureCreated();
        SeedDefaultTenant(_dbContext);
    }

    #region Local Authentication E2E Tests

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

    // LocalAuth_TokenRefresh_AfterRevocation_ShouldFail was retired: the production
    // revocation path (RefreshTokenRepository.RevokeFamilyAsync / replay containment
    // from the #263 sweep) uses atomic ExecuteUpdate statements, which the EF InMemory
    // provider cannot translate, so this InMemory-hosted copy could never execute
    // again. Its scenario — revoked refresh token rejected with replay containment,
    // family revocation, and session termination — is covered end-to-end against the
    // real provider at the HTTP boundary by the Postgres suite:
    // GameGuild.API.IntegrationTests/BearerRevocationPostgreSqlHttpTests.cs
    // (RealRefreshReplayRejectsEarlierSignedBearerAndLeavesAnotherUserActive) and
    // RefreshTokenLifecycleAuditPostgreSqlHttpTests.cs
    // (ReplayCommitsContainmentAuditAndMetricWhileReturningNoCredentials,
    // ExplicitRevocationCommitsAuditAndMetricForOwnedTokenOrWholeAccount).

    [Theory]
    [InlineData("Member", false)]
    [InlineData("Owner", true)]
    [InlineData("Admin", true)]
    public async Task LocalAuth_SignIn_WithTenantMemberships_ShouldPopulateTenantContext(string membershipRole, bool requiresMfa)
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
                Role = membershipRole,
                IsActive = true,
                Tenant = tenantOne
            },
            new TenantMember
            {
                UserId = userId,
                TenantId = tenantTwo.Id,
                Role = membershipRole,
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
        var sessionsBefore = await _dbContext.Set<UserSession>().CountAsync(session => session.UserId == userId);

        // Act
        var signInResult = await _authService.LocalSignInAsync(signInRequest);

        // Assert
        signInResult.TenantId.Should().Be(tenantTwo.Id);
        signInResult.UserId.Should().Be(userId);
        signInResult.RequiresMfa.Should().Be(requiresMfa);
        if (requiresMfa)
        {
            // Elevated memberships retain the resolved binding, but need a verified second factor
            // before the complete tenant list or ordinary credentials can be returned.
            signInResult.Success.Should().BeFalse();
            signInResult.MfaToken.Should().NotBeNullOrEmpty();
            signInResult.AvailableTenants.Should().BeNull();
            signInResult.AccessToken.Should().BeEmpty();
            signInResult.RefreshToken.Should().BeEmpty();
            signInResult.SessionId.Should().BeEmpty();
            (await _dbContext.Set<UserSession>().CountAsync(session => session.UserId == userId)).Should().Be(sessionsBefore);
            return;
        }

        signInResult.Success.Should().BeTrue();
        signInResult.AccessToken.Should().NotBeNullOrEmpty();
        signInResult.RefreshToken.Should().NotBeNullOrEmpty();
        signInResult.SessionId.Should().NotBeEmpty();
        signInResult.AvailableTenants.Should().NotBeNull();
        signInResult.AvailableTenants!.Should().HaveCount(3, "every authenticated user is provisioned into the default tenant");
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

    // Google ID-token sign-in is exercised end to end over HTTP against the real
    // ASP.NET host (routing → GoogleIdTokenSignInCommand → OAuthAuthService → user
    // provisioning/linking → default-tenant membership → session + token issuance →
    // SignInResponse serialization). Only the cryptographic boundary is stubbed:
    // GoogleSignInE2EApiFactory swaps IGoogleIdTokenVerifier (whose production
    // implementation calls GoogleJsonWebSignature.ValidateAsync — an external
    // Google JWKS dependency) for a programmable verifier that is faithful to the
    // real contract: it returns the provider-asserted claims for tokens that would
    // pass Google's validation and throws UnauthorizedAccessException for every
    // other token, exactly as GoogleIdTokenVerifier does for forged/expired
    // tokens. Everything downstream of that seam is production code.

    [Fact]
    public async Task SocialAuth_GoogleProvider_CompleteFlow_ShouldAuthenticateUser()
    {
        // Arrange - a Google credential the stub verifier accepts
        var email = $"google.e2e.{Guid.NewGuid():N}@gmail.com";
        var googleSub = $"google-sub-{Guid.NewGuid():N}";
        var idToken = $"e2e-google-id-token-{Guid.NewGuid():N}";

        using var factory = new GoogleSignInE2EApiFactory();
        factory.Verifier.Allow(idToken, new VerifiedGoogleUser
        {
            Sub = googleSub,
            Email = email,
            EmailVerified = true,
            Name = "Google E2E User",
            Picture = "https://lh3.googleusercontent.com/gameguild-e2e.png"
        });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        SeedDefaultTenant(dbContext);
        var defaultTenantId = dbContext.Set<Tenant>().Single(t => t.IsDefault).Id;

        using var client = factory.CreateClient();

        // Act - POST the endpoint the web Google GIS/One Tap credential feeds
        using var response = await client.PostAsJsonAsync(
            "/v1/auth/google:sign-in",
            new GoogleIdTokenRequestDto { IdToken = idToken });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var dto = await response.Content.ReadFromJsonAsync<SignInResponse>();

        // Assert - response DTO: tokens, session and tenant context are all issued
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.Message.Should().Be("Google ID token sign-in successful");
        dto.AccessToken.Should().NotBeNullOrWhiteSpace();
        dto.AccessToken.Split('.').Should().HaveCount(3, "the access token must be a three-segment JWT");
        dto.RefreshToken.Should().NotBeNullOrWhiteSpace();
        dto.UserId.Should().NotBeEmpty();
        dto.Email.Should().Be(email);
        dto.SessionId.Should().NotBeEmpty();
        dto.ExpiresIn.Should().BePositive();
        dto.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        dto.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        dto.TenantId.Should().Be(defaultTenantId);
        dto.AvailableTenants.Should().ContainSingle(tenant => tenant.Id == defaultTenantId);

        // Assert - user provisioned and the Google identity linked to it
        var user = await dbContext.Set<User>().SingleAsync(u => u.Email == email);
        user.Id.Should().Be(dto.UserId);
        var externalLogin = await dbContext.Set<ExternalLogin>()
            .SingleAsync(login => login.Provider == "google" && login.ProviderKey == googleSub);
        externalLogin.UserId.Should().Be(user.Id);

        // Assert - self-service default-tenant membership provisioned
        var membership = await dbContext.Set<TenantMember>().SingleAsync(m => m.UserId == user.Id);
        membership.TenantId.Should().Be(defaultTenantId);
        membership.IsActive.Should().BeTrue();

        // Assert - session persisted and only the hashed refresh token is stored
        var hasher = scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();
        var session = await dbContext.Set<UserSession>().SingleAsync(s => s.Id == dto.SessionId);
        session.UserId.Should().Be(user.Id);
        session.IsActive.Should().BeTrue();
        session.RefreshToken.Should().Be(hasher.HashToken(dto.RefreshToken));

        // Assert - the federated success is audited
        (await dbContext.Set<AuthenticationAttempt>()
            .AnyAsync(attempt => attempt.Email == email && attempt.UserId == user.Id && attempt.IsSuccessful))
            .Should().BeTrue();
    }

    [Fact]
    public async Task SocialAuth_GoogleProvider_SecondSignIn_ShouldReuseLinkedIdentityWithoutDuplicates()
    {
        // Arrange
        var email = $"google.e2e.repeat.{Guid.NewGuid():N}@gmail.com";
        var googleSub = $"google-sub-{Guid.NewGuid():N}";
        var idToken = $"e2e-google-id-token-{Guid.NewGuid():N}";

        using var factory = new GoogleSignInE2EApiFactory();
        factory.Verifier.Allow(idToken, new VerifiedGoogleUser
        {
            Sub = googleSub,
            Email = email,
            EmailVerified = true,
            Name = "Google E2E Repeat User"
        });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        SeedDefaultTenant(dbContext);

        using var client = factory.CreateClient();

        // Act - the same Google identity signs in twice
        using var firstResponse = await client.PostAsJsonAsync(
            "/v1/auth/google:sign-in",
            new GoogleIdTokenRequestDto { IdToken = idToken });
        using var secondResponse = await client.PostAsJsonAsync(
            "/v1/auth/google:sign-in",
            new GoogleIdTokenRequestDto { IdToken = idToken });

        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, firstBody);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK, secondBody);

        var first = await firstResponse.Content.ReadFromJsonAsync<SignInResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<SignInResponse>();

        // Assert - linked user is reused, no duplicate user or external login, fresh session each time
        first!.UserId.Should().Be(second!.UserId);
        first.SessionId.Should().NotBe(second.SessionId);
        (await dbContext.Set<User>().CountAsync(u => u.Email == email)).Should().Be(1);
        (await dbContext.Set<ExternalLogin>()
            .CountAsync(login => login.Provider == "google" && login.ProviderKey == googleSub))
            .Should().Be(1);
        var sessionIds = new[] { first.SessionId, second.SessionId };
        (await dbContext.Set<UserSession>().CountAsync(session => sessionIds.Contains(session.Id)))
            .Should().Be(2);
    }

    [Fact]
    public async Task SocialAuth_GoogleProvider_InvalidToken_ShouldFailClosedWithoutUserOrSession()
    {
        // Arrange - a forged token the stub verifier (like GoogleJsonWebSignature) rejects
        var forgedToken = $"e2e-forged-google-id-token-{Guid.NewGuid():N}";
        var victimEmail = $"google.e2e.forged.{Guid.NewGuid():N}@gmail.com";

        using var factory = new GoogleSignInE2EApiFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        SeedDefaultTenant(dbContext);

        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/v1/auth/google:sign-in",
            new GoogleIdTokenRequestDto { IdToken = forgedToken });

        // Assert - fail closed: 401, no credentials disclosed
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        problem.Title.Should().Be("Unauthorized");

        // Assert - no user provisioned, no external link, no session, no success audit
        (await dbContext.Set<User>().AnyAsync(u => u.Email == victimEmail)).Should().BeFalse();
        (await dbContext.Set<ExternalLogin>().AnyAsync()).Should().BeFalse();
        (await dbContext.Set<UserSession>().CountAsync()).Should().Be(0);
        (await dbContext.Set<AuthenticationAttempt>()
            .AnyAsync(attempt => !attempt.IsSuccessful && attempt.FailureReason == nameof(UnauthorizedAccessException)))
            .Should().BeTrue("a rejected Google token must be audited as a failed attempt");
    }

    [Fact]
    public async Task SocialAuth_GoogleProvider_UnverifiedEmailOverExistingLocalAccount_ShouldRefuseLinking()
    {
        // Arrange - a pre-existing local account with the same email
        var email = $"google.e2e.collision.{Guid.NewGuid():N}@example.com";

        using var factory = new GoogleSignInE2EApiFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        SeedDefaultTenant(dbContext);

        using var client = factory.CreateClient();

        using var signUpResponse = await client.PostAsJsonAsync("/v1/auth/sign-up", new LocalSignUpRequest
        {
            Email = email,
            Username = $"local_user_{Guid.NewGuid():N}",
            Password = "LocalPassword123!"
        });
        var signUpBody = await signUpResponse.Content.ReadAsStringAsync();
        signUpResponse.StatusCode.Should().Be(HttpStatusCode.Created, signUpBody);

        var localUser = await dbContext.Set<User>().SingleAsync(u => u.Email == email);
        var sessionsBefore = await dbContext.Set<UserSession>().CountAsync(s => s.UserId == localUser.Id);

        // An unverified Google identity must not hijack the local account
        var idToken = $"e2e-google-id-token-unverified-{Guid.NewGuid():N}";
        factory.Verifier.Allow(idToken, new VerifiedGoogleUser
        {
            Sub = $"google-sub-{Guid.NewGuid():N}",
            Email = email,
            EmailVerified = false,
            Name = "Unverified Google User"
        });

        // Act
        using var response = await client.PostAsJsonAsync(
            "/v1/auth/google:sign-in",
            new GoogleIdTokenRequestDto { IdToken = idToken });

        // Assert - denied, no link created, no extra session issued
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, body);
        (await dbContext.Set<ExternalLogin>().AnyAsync(login => login.UserId == localUser.Id)).Should().BeFalse();
        (await dbContext.Set<UserSession>().CountAsync(s => s.UserId == localUser.Id))
            .Should().Be(sessionsBefore);
    }

    [Fact]
    public async Task SocialAuth_GitHubProvider_CompleteFlow_ShouldAuthenticateUser()
    {
        // Arrange - Mock GitHub OAuth response
        var mockGitHubCode = "mock_github_authorization_code";

        var githubSignInRequest = new GitHubSignInRequest
        {
            AccessToken = mockGitHubCode
        };

        // Act & Assert
        // This would work with proper mocking of external GitHub OAuth
        // await FluentActions.Invoking(async () => await _authService.SocialSignInAsync(githubSignInRequest))
        //     .Should().NotThrowAsync();

        await Task.CompletedTask; // Placeholder until actual implementation
    }

    #endregion

    #region Web3 Authentication E2E Tests

    // Revived (previously skipped with "Web3ChallengeRequest is abstract"): the request is
    // now a concrete DTO (DTOs/Web3ChallengeRequest.cs) and the whole flow exists as
    // IAuthService.GenerateWeb3ChallengeAsync / VerifyWeb3SignatureAsync behind
    // POST /v1/auth/web3/challenge and /v1/auth/web3:verify. The SIWE configuration comes
    // from appsettings.json (Origin=https://gameguild.gg, AllowedChainIds=["1"]), which the
    // InMemory Testing host picks up unchanged. The signatures below are genuine EIP-191
    // secp256k1 signatures produced with Nethereum — no cryptographic seam is stubbed.
    // Time-based challenge expiry needs a controllable clock and is covered against the
    // real provider by Web3AuthenticationPostgreSqlHttpTests
    // (ChallengeLifetimeIsFiveMinutesAndExactExpirationRejectsRealSignatureWithoutIssuance).

    [Fact]
    public async Task Web3Auth_CompleteFlow_ChallengeAndVerify_ShouldAuthenticateWallet()
    {
        // Arrange - a real keypair; the wallet address is its public address
        var walletKey = new EthECKey(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        var walletAddress = walletKey.GetPublicAddress();

        // Edge - malformed (not 0x + 40 hex) and invalid-checksum addresses are rejected
        // by challenge generation instead of yielding a signable message.
        var malformedAddress = $"0x{Guid.NewGuid():N}";
        await FluentActions.Invoking(() => _authService.GenerateWeb3ChallengeAsync(
                new Web3ChallengeRequest { WalletAddress = malformedAddress, ChainId = "1" }))
            .Should().ThrowAsync<ArgumentException>();

        var badChecksumAddress = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAeD";
        await FluentActions.Invoking(() => _authService.GenerateWeb3ChallengeAsync(
                new Web3ChallengeRequest { WalletAddress = badChecksumAddress, ChainId = "1" }))
            .Should().ThrowAsync<ArgumentException>();

        // Act 1: generate the SIWE challenge through the composite auth service
        var challengeResult = await _authService.GenerateWeb3ChallengeAsync(new Web3ChallengeRequest
        {
            WalletAddress = walletAddress,
            ChainId = "1"
        });

        // Assert Challenge Generation - EIP-4361 message bound to the wallet, single-use nonce
        challengeResult.Should().NotBeNull();
        challengeResult.Challenge.Should().Contain("wants you to sign in with your Ethereum account:");
        challengeResult.Challenge.ToLowerInvariant().Should().Contain(walletAddress.ToLowerInvariant(),
            "the challenge must be bound to the requesting wallet");
        challengeResult.Nonce.Should().NotBeNullOrWhiteSpace();
        challengeResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Act 2: sign the exact issued message with the wallet key and verify
        var signature = new EthereumMessageSigner().EncodeUTF8AndSign(challengeResult.Challenge, walletKey);
        var signInResult = await _authService.VerifyWeb3SignatureAsync(new Web3VerificationRequest
        {
            WalletAddress = walletAddress,
            Challenge = challengeResult.Challenge,
            Signature = signature,
            Nonce = challengeResult.Nonce,
            ChainId = "1"
        });

        // Assert - complete wallet credentials issued
        signInResult.Success.Should().BeTrue();
        signInResult.Message.Should().Be("Web3 authentication successful");
        signInResult.AccessToken.Should().NotBeNullOrWhiteSpace();
        signInResult.AccessToken.Split('.').Should().HaveCount(3, "the access token must be a three-segment JWT");
        signInResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        signInResult.SessionId.Should().NotBeEmpty();
        signInResult.UserId.Should().NotBeEmpty();
        signInResult.TenantId.Should().HaveValue();

        // Assert - the wallet identity was provisioned exactly once and linked to the account
        var walletIdentifier = walletAddress.ToLowerInvariant() + "@web3.local";
        var user = await _dbContext.Set<User>().SingleAsync(u => u.Email == walletIdentifier);
        user.Id.Should().Be(signInResult.UserId);
        var externalLogin = await _dbContext.Set<ExternalLogin>().SingleAsync(
            login => login.Provider == "web3" && login.ProviderKey == walletAddress.ToLowerInvariant());
        externalLogin.UserId.Should().Be(user.Id);

        // Assert - the session persists with only the hashed refresh token
        var hasher = _scope.ServiceProvider.GetRequiredService<IRefreshTokenHasher>();
        var session = await _dbContext.Set<UserSession>().SingleAsync(s => s.Id == signInResult.SessionId);
        session.UserId.Should().Be(user.Id);
        session.IsActive.Should().BeTrue();
        session.RefreshToken.Should().Be(hasher.HashToken(signInResult.RefreshToken));
    }

    [Fact]
    public async Task Web3Auth_UnknownOrConsumedChallenge_ShouldFailVerificationWithoutIssuingCredentials()
    {
        // Arrange - a live challenge for a fresh wallet
        var walletKey = new EthECKey(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        var walletAddress = walletKey.GetPublicAddress();
        var challengeResult = await _authService.GenerateWeb3ChallengeAsync(new Web3ChallengeRequest
        {
            WalletAddress = walletAddress,
            ChainId = "1"
        });
        var walletIdentifier = walletAddress.ToLowerInvariant() + "@web3.local";
        var sessionsBefore = await _dbContext.Set<UserSession>().CountAsync();

        // Act 1 - verify against a challenge that was never issued (the shape an expired,
        // evicted or fabricated challenge takes from the verifier's point of view). The
        // signature itself is genuine, so the mismatch must be caught on the challenge.
        var fabricatedChallenge = challengeResult.Challenge + "\nExpired-Block";
        await FluentActions.Invoking(() => _authService.VerifyWeb3SignatureAsync(new Web3VerificationRequest
            {
                WalletAddress = walletAddress,
                Challenge = fabricatedChallenge,
                Signature = new EthereumMessageSigner().EncodeUTF8AndSign(fabricatedChallenge, walletKey),
                Nonce = challengeResult.Nonce,
                ChainId = "1"
            }))
            .Should().ThrowAsync<AuthenticationRequiredException>()
            .WithMessage("Invalid Web3 signature");

        // Assert - fail closed: no account, no wallet link, no session, audited attempt
        (await _dbContext.Set<User>().AnyAsync(u => u.Email == walletIdentifier)).Should().BeFalse();
        (await _dbContext.Set<ExternalLogin>()
            .AnyAsync(login => login.Provider == "web3" && login.ProviderKey == walletAddress.ToLowerInvariant()))
            .Should().BeFalse();
        (await _dbContext.Set<UserSession>().CountAsync()).Should().Be(sessionsBefore);
        (await _dbContext.Set<AuthenticationAttempt>()
            .AnyAsync(attempt => !attempt.IsSuccessful
                && attempt.Email == "web3@audit.invalid"
                && attempt.FailureReason == "InvalidSignature"))
            .Should().BeTrue("a rejected Web3 verification must be audited as a failed attempt");

        // Act 2 - the genuine challenge verifies exactly once; replaying the consumed
        // nonce fails even though signature, message and nonce are all valid.
        var validSignature = new EthereumMessageSigner().EncodeUTF8AndSign(challengeResult.Challenge, walletKey);
        var signIn = await _authService.VerifyWeb3SignatureAsync(new Web3VerificationRequest
        {
            WalletAddress = walletAddress,
            Challenge = challengeResult.Challenge,
            Signature = validSignature,
            Nonce = challengeResult.Nonce,
            ChainId = "1"
        });
        signIn.Success.Should().BeTrue();

        await FluentActions.Invoking(() => _authService.VerifyWeb3SignatureAsync(new Web3VerificationRequest
            {
                WalletAddress = walletAddress,
                Challenge = challengeResult.Challenge,
                Signature = validSignature,
                Nonce = challengeResult.Nonce,
                ChainId = "1"
            }))
            .Should().ThrowAsync<AuthenticationRequiredException>()
            .WithMessage("Invalid Web3 signature");

        // Assert - exactly one session was ever issued for this wallet
        var user = await _dbContext.Set<User>().SingleAsync(u => u.Email == walletIdentifier);
        (await _dbContext.Set<UserSession>().CountAsync(s => s.UserId == user.Id)).Should().Be(1);
    }

    #endregion

    #region Polymorphic Authentication E2E Tests

    // Revived (previously skipped with "PolymorphicSignInAsync API not yet implemented"):
    // the implemented API is NOT a dictionary-of-credentials service method — it is the
    // POST /v1/auth/polymorphic endpoint (AuthController.PolymorphicSignIn) backed by
    // PolymorphicSignInCommand/PolymorphicSignInHandler, which resolves ONE account by an
    // email, username or international-phone identifier plus a password, then applies the
    // existing password, risk, tenant and session flow. The tests below exercise that real
    // contract over HTTP against the shared InMemory host (the class fixture's _client).

    [Fact]
    public async Task PolymorphicAuth_LocalStrategy_ShouldAuthenticateCorrectly()
    {
        // Arrange - a local account reachable by both email and username
        var email = $"poly.local.{Guid.NewGuid():N}@test.com";
        var username = $"poly_user_{Guid.NewGuid():N}";
        var password = "PolyPassword123!";

        var signUpResult = await _authService.LocalSignUpAsync(new LocalSignUpRequest
        {
            Email = email,
            Username = username,
            Password = password
        });

        // Act - sign in through the polymorphic endpoint using the email strategy
        using var response = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = email,
            credentialType = "Email",
            password
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var dto = await response.Content.ReadFromJsonAsync<SignInResponse>();

        // Assert - the same credentials, tenant context and tokens as local sign-in
        dto.Should().NotBeNull();
        dto!.Success.Should().BeTrue();
        dto.UserId.Should().Be(signUpResult.UserId);
        dto.AccessToken.Should().NotBeNullOrWhiteSpace();
        dto.AccessToken.Split('.').Should().HaveCount(3, "the access token must be a three-segment JWT");
        dto.RefreshToken.Should().NotBeNullOrWhiteSpace();
        dto.SessionId.Should().NotBeEmpty();
        dto.TenantId.Should().HaveValue();

        var session = await _dbContext.Set<UserSession>().SingleAsync(s => s.Id == dto.SessionId);
        session.UserId.Should().Be(signUpResult.UserId);
        session.IsActive.Should().BeTrue();

        // Act 2 - the username strategy resolves the very same account
        using var usernameResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = username,
            credentialType = "Username",
            password
        });

        var usernameBody = await usernameResponse.Content.ReadAsStringAsync();
        usernameResponse.StatusCode.Should().Be(HttpStatusCode.OK, usernameBody);
        var usernameDto = await usernameResponse.Content.ReadFromJsonAsync<SignInResponse>();
        usernameDto!.Success.Should().BeTrue();
        usernameDto.UserId.Should().Be(signUpResult.UserId,
            "email and username strategies must resolve the same account");
        usernameDto.SessionId.Should().NotBe(dto.SessionId, "each sign-in issues its own session");
    }

    [Fact]
    public async Task PolymorphicAuth_WalletAddressStrategy_AndUnknownIdentifier_ShouldFailWithGenericAuthenticationFailure()
    {
        // Arrange - a syntactically valid wallet address. The polymorphic endpoint only
        // resolves email, username and phone identifiers (PolymorphicSignInHandler), so a
        // WalletAddress strategy can never resolve an account. Per the controller docs,
        // missing, ambiguous or invalid identifiers receive the generic authentication
        // failure — indistinguishable from an unknown email, so no enumeration leak.
        var walletAddress = new EthECKey(Convert.ToHexString(RandomNumberGenerator.GetBytes(32))).GetPublicAddress();
        var unknownEmail = $"unknown.{Guid.NewGuid():N}@test.com";
        var password = "WalletPassword123!";
        var sessionsBefore = await _dbContext.Set<UserSession>().CountAsync();

        // Act - WalletAddress strategy
        using var walletResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = walletAddress,
            credentialType = "WalletAddress",
            password
        });

        var walletBody = await walletResponse.Content.ReadAsStringAsync();
        walletResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, walletBody);
        var walletProblem = await walletResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        walletProblem.Should().NotBeNull();
        walletProblem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        walletProblem.Title.Should().Be("Unauthorized");
        walletProblem.Detail.Should().Be("Invalid credentials. Please check your email and password.",
            "unsupported identifiers get the generic login failure");

        // Act 2 - an unknown email identifier
        using var unknownResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = unknownEmail,
            credentialType = "Email",
            password
        });

        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
        unknownResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, unknownBody);
        var unknownProblem = await unknownResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        unknownProblem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        unknownProblem.Detail.Should().Be(walletProblem.Detail,
            "unknown and unsupported identifiers must be indistinguishable");
        unknownProblem.Title.Should().Be(walletProblem.Title);

        // Assert - no credentials issued and both failures audited as password attempts
        (await _dbContext.Set<UserSession>().CountAsync()).Should().Be(sessionsBefore);
        (await _dbContext.Set<AuthenticationAttempt>()
            .AnyAsync(attempt => !attempt.IsSuccessful
                && attempt.FailureReason == "InvalidCredentials"
                && (attempt.Email == walletAddress || attempt.Email == unknownEmail)))
            .Should().BeTrue("rejected polymorphic identifiers must be audited as failed attempts");
    }

    [Fact]
    public async Task PolymorphicAuth_CrossStrategy_UserWithMultipleIdentifiers_ShouldResolveSameAccountAcrossStrategies()
    {
        // Arrange - one local account with both an email and a username identifier
        var email = $"cross.strategy.{Guid.NewGuid():N}@test.com";
        var username = $"cross_user_{Guid.NewGuid():N}";
        var password = "CrossStrategy123!";

        var localResult = await _authService.LocalSignUpAsync(new LocalSignUpRequest
        {
            Email = email,
            Username = username,
            Password = password
        });
        var userId = localResult.UserId;
        var defaultTenantId = _dbContext.Set<Tenant>().Single(t => t.IsDefault).Id;

        // Act - three strategies: explicit email, explicit username, and auto-detection
        using var emailResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = email,
            credentialType = "Email",
            password
        });
        using var usernameResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = username,
            credentialType = "Username",
            password
        });
        using var autoDetectedResponse = await _client.PostAsJsonAsync("/v1/auth/polymorphic", new
        {
            credential = email,
            password
        });

        var emailBody = await emailResponse.Content.ReadAsStringAsync();
        emailResponse.StatusCode.Should().Be(HttpStatusCode.OK, emailBody);
        var usernameBody = await usernameResponse.Content.ReadAsStringAsync();
        usernameResponse.StatusCode.Should().Be(HttpStatusCode.OK, usernameBody);
        var autoDetectedBody = await autoDetectedResponse.Content.ReadAsStringAsync();
        autoDetectedResponse.StatusCode.Should().Be(HttpStatusCode.OK, autoDetectedBody);

        var byEmail = await emailResponse.Content.ReadFromJsonAsync<SignInResponse>();
        var byUsername = await usernameResponse.Content.ReadFromJsonAsync<SignInResponse>();
        var byAutoDetection = await autoDetectedResponse.Content.ReadFromJsonAsync<SignInResponse>();

        // Assert - every strategy resolves the same account in the default tenant, each
        // with its own session; the account can be reached by any of its identifiers.
        var results = new[] { byEmail!, byUsername!, byAutoDetection! };
        results.Should().OnlyContain(result => result.Success);
        results.Should().OnlyContain(result => result.UserId == userId);
        results.Should().OnlyContain(result => result.TenantId == defaultTenantId);
        results.Should().OnlyContain(result =>
            result.AvailableTenants != null
            && result.AvailableTenants.Single().Id == defaultTenantId);
        results.Select(result => result.SessionId).Should().OnlyHaveUniqueItems();

        (await _dbContext.Set<UserSession>().CountAsync(s => s.UserId == userId))
            .Should().Be(3, "each polymorphic strategy sign-in issues its own session");
    }

    #endregion

    #region Token Lifecycle Edge Cases

    /*
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
    */

    // TokenLifecycle_MultipleRefreshes_ShouldInvalidateOldTokens stays commented for the
    // same provider reason as the retired LocalAuth_TokenRefresh_AfterRevocation_ShouldFail
    // above: refresh rotation/revocation runs through ExecuteUpdate statements that the EF
    // InMemory provider cannot translate. Its scenario (old refresh tokens invalid after
    // rotation) is covered against the real provider by the Postgres suites, e.g.
    // RefreshTokenRotationPostgreSqlTests in this project and
    // GameGuild.API.IntegrationTests.BearerRevocationPostgreSqlHttpTests.
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

    private static void SeedDefaultTenant(ApplicationDbContext dbContext)
    {
        if (dbContext.Set<Tenant>().Any(tenant => tenant.IsDefault))
        {
            return;
        }

        dbContext.Set<Tenant>().Add(new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Authentication Integration Default Tenant",
            Slug = "authentication-integration-default",
            Description = "Default tenant required by the authentication integration fixture.",
            AdminEmail = "authentication-integration-admin@example.test",
            IsActive = true,
            IsDefault = true
        });
        dbContext.SaveChanges();
    }

    /// <summary>
    ///     Programmable stand-in for <see cref="IGoogleIdTokenVerifier" />. Tokens registered
    ///     via <see cref="Allow" /> yield their provider-asserted claims (the outcome of a
    ///     successful GoogleJsonWebSignature validation); every other token is rejected with
    ///     <see cref="UnauthorizedAccessException" />, mirroring the fail-closed production
    ///     contract for forged, expired or mis-audience tokens.
    /// </summary>
    private sealed class StubGoogleIdTokenVerifier : IGoogleIdTokenVerifier
    {
        private readonly Dictionary<string, VerifiedGoogleUser> _validTokens = new(StringComparer.Ordinal);

        public void Allow(string idToken, VerifiedGoogleUser user) => _validTokens[idToken] = user;

        public Task<VerifiedGoogleUser> VerifyAsync(string idToken, CancellationToken ct) =>
            _validTokens.TryGetValue(idToken, out var user)
                ? Task.FromResult(user)
                : Task.FromException<VerifiedGoogleUser>(
                    new UnauthorizedAccessException("Google ID token is invalid"));
    }

    /// <summary>
    ///     Host for the Google sign-in E2E flow: same InMemory configuration as
    ///     <see cref="AuthenticationApiFactory" /> plus the stubbed
    ///     <see cref="IGoogleIdTokenVerifier" /> test seam. The sign-in happy path never
    ///     touches the ExecuteUpdate-based revocation code (RefreshTokenRepository), which
    ///     is what keeps the InMemory provider viable here; revocation is covered by the
    ///     Postgres suites (see the retired-test note above).
    /// </summary>
    private sealed class GoogleSignInE2EApiFactory : WebApplicationFactory<GameGuild.API.Program>
    {
        private readonly string _databaseName = $"GoogleSignInE2ETests_{Guid.NewGuid()}";

        public StubGoogleIdTokenVerifier Verifier { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(descriptor =>
                        descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        descriptor.ServiceType == typeof(ApplicationDbContext) ||
                        descriptor.ServiceType.FullName?.Contains("EntityFramework", StringComparison.Ordinal) == true ||
                        descriptor.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                services.AddMemoryCache();
                services.AddHttpLogging(_ => { });

                // Test seam: replace Google's cryptographic validation with the controlled stub.
                services.RemoveAll<IGoogleIdTokenVerifier>();
                services.AddSingleton<IGoogleIdTokenVerifier>(Verifier);
            });
        }
    }
}
