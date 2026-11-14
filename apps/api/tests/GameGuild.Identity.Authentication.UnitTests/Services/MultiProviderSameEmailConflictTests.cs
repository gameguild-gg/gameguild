using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication.UnitTests.Infrastructure;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Multiple OIDC/OAuth providers returning the same email (issue #1121): federated
///     identities resolve by (provider, providerKey), never by email alone. When a second
///     provider's verified email collides with an account that was provisioned by a
///     DIFFERENT provider, the sign-in must stop with an explicit linking challenge —
///     never a silent merge (one provider's email-holder inheriting another's account) and
///     never a duplicate account for the same federated human. Local password accounts keep
///     the documented verified-email auto-link behavior.
/// </summary>
public class MultiProviderSameEmailConflictTests
{
    private const string SharedEmail = "shared@example.com";

    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock = new();
    private readonly Mock<IOAuthService> _oauthServiceMock = new();
    private readonly Mock<IGoogleIdTokenVerifier> _googleVerifierMock = new();
    private readonly Mock<IOidcFederationService> _oidcFederationServiceMock = new();
    private readonly Mock<IMfaService> _mfaServiceMock = new();
    private readonly Mock<IExternalLoginRepository> _externalLoginRepoMock = new();
    private readonly Mock<IAuthAttemptService> _authAttemptServiceMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ISessionManagementService> _sessionManagementServiceMock = new();
    private readonly IConfiguration _configuration;
    private readonly List<User> _createdUsers = [];
    private readonly List<ExternalLogin> _upsertedLinks = [];

    public MultiProviderSameEmailConflictTests()
    {
        PersistedAuthenticationSessions.Configure(_sessionManagementServiceMock);
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenExpiryInDays", "7" },
                { "Jwt:RefreshTokenExpirationDays", "7" },
                { "Jwt:AccessTokenExpirationMinutes", "60" }
            })
            .Build();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authAttemptServiceMock.Setup(x => x.GetClientIpAddress(It.IsAny<HttpContext>())).Returns("127.0.0.1");
        _refreshTokenHasherMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns((string token) => $"hash-{token}");
        _mfaServiceMock
            .Setup(x => x.IsMfaRequiredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetUserMembershipsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserMembershipsResponse());
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => _createdUsers.Add(user))
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _externalLoginRepoMock
            .Setup(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalLogin, CancellationToken>((link, _) => _upsertedLinks.Add(link))
            .ReturnsAsync((ExternalLogin link, CancellationToken _) => link);
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);

        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("github", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile
            {
                ProviderId = "github-node-1",
                Provider = "GitHub",
                Email = SharedEmail,
                EmailVerified = true,
                Name = "Shared Human",
                Username = "sharedhuman",
                AccessToken = "github-access-token"
            });
        _oauthServiceMock
            .Setup(x => x.ResolveAuthorizationScopes(It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns(new[] { "read:user", "user:email" });
    }

    private OAuthAuthService CreateSut() => new(
        _userRepoMock.Object,
        _jwtTokenServiceMock.Object,
        _refreshTokenHasherMock.Object,
        _oauthServiceMock.Object,
        _googleVerifierMock.Object,
        _oidcFederationServiceMock.Object,
        _mfaServiceMock.Object,
        _externalLoginRepoMock.Object,
        _configuration,
        _authAttemptServiceMock.Object,
        _httpContextAccessorMock.Object,
        _senderMock.Object,
        _sessionManagementServiceMock.Object,
        NullLogger<OAuthAuthService>.Instance);

    private User ExistingFederatedAccount(string provider, string providerKey)
    {
        var existing = User.CreateOAuthUser(SharedEmail, "Shared Human");
        _userRepoMock
            .Setup(x => x.GetByEmailAsync(SharedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _userRepoMock
            .Setup(x => x.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalLogin { Id = Guid.NewGuid(), UserId = existing.Id, Provider = provider, ProviderKey = providerKey }]);
        return existing;
    }

    private static GitHubCallbackSignInRequest GitHubRequest() =>
        new() { Code = "github-auth-code", State = "state-123", RedirectUri = "https://web.example.com/api/auth/callback/github" };

    private void OidcCallbackReturns(string slug, string providerKey) =>
        _oidcFederationServiceMock
            .Setup(x => x.AuthenticateCallbackAsync(slug, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OidcFederatedIdentity
            {
                Slug = slug,
                ProviderKey = providerKey,
                Email = SharedEmail,
                EmailVerified = true,
                Name = "Shared Human"
            });

    // ── (1) Different provider, same email → explicit linking challenge, no merge ────

    [Fact]
    public async Task GitHubSignIn_EmailOwnedByGoogleAccount_RequiresExplicitLink_NoSilentMerge()
    {
        ExistingFederatedAccount("google", "google-subject-1");
        var sut = CreateSut();

        var act = () => sut.GitHubCallbackSignInAsync(GitHubRequest());

        // The challenge is the 401-mapped typed exception — clients get a linking
        // instruction, and nothing about the existing account changed.
        var exception = (await act.Should().ThrowAsync<FederatedAccountLinkRequiredException>())
            .Which;
        exception.Provider.Should().Be("github");
        exception.ExistingProvider.Should().Be("google");
        exception.Email.Should().Be(SharedEmail);

        _createdUsers.Should().BeEmpty("no second account may be minted for the same email");
        _upsertedLinks.Should().BeEmpty("the GitHub identity must not be grafted onto the Google account");
    }

    // ── (2) Two generic OIDC providers (Google + Auth0) with the same email ───────────

    [Fact]
    public async Task OidcSignIn_SecondProviderSameEmailAsFirstProvider_RequiresExplicitLink()
    {
        ExistingFederatedAccount("oidc-google", "google-subject-1");
        OidcCallbackReturns("auth0", "auth0-subject-1");
        var sut = CreateSut();

        var act = () => sut.OidcSignInAsync(new OidcSignInRequest
        {
            Slug = "auth0",
            Code = "oidc-auth-code",
            State = "state-456",
            RedirectUri = "https://web.example.com/api/auth/callback/oidc"
        });

        (await act.Should().ThrowAsync<FederatedAccountLinkRequiredException>())
            .Which.ExistingProvider.Should().Be("oidc-google");

        _createdUsers.Should().BeEmpty();
        _upsertedLinks.Should().BeEmpty("the Auth0 identity must not silently join the oidc-google account");
    }

    // ── (3) Sequential first sign-ins: one account per provider, challenge on collision ─

    [Fact]
    public async Task TwoProviderCallbacksSameEmail_FirstCreatesAccount_SecondIsChallenged()
    {
        // First provider callback: nothing exists — JIT provisions exactly one account.
        _userRepoMock
            .Setup(x => x.GetByEmailAsync(SharedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        OidcCallbackReturns("google", "google-subject-1");
        var sut = CreateSut();

        var first = await sut.OidcSignInAsync(new OidcSignInRequest
        {
            Slug = "google",
            Code = "oidc-auth-code",
            State = "state-456",
            RedirectUri = "https://web.example.com/api/auth/callback/oidc"
        });

        first.Success.Should().BeTrue();
        _createdUsers.Should().HaveCount(1, "exactly one account exists for the email after the first provider");
        _upsertedLinks.Should().ContainSingle(link => link.Provider == "oidc-google");

        // Second provider callback with the same email: the first account now owns the
        // address through its oidc-google link — the second provider gets the challenge.
        var googleAccount = _createdUsers[0];
        _userRepoMock
            .Setup(x => x.GetByEmailAsync(SharedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleAccount);
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(googleAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_upsertedLinks.ToList());
        var createdBefore = _createdUsers.Count;
        var upsertedBefore = _upsertedLinks.Count;

        OidcCallbackReturns("auth0", "auth0-subject-1");
        var act = () => sut.OidcSignInAsync(new OidcSignInRequest
        {
            Slug = "auth0",
            Code = "oidc-auth-code-2",
            State = "state-789",
            RedirectUri = "https://web.example.com/api/auth/callback/oidc"
        });

        await act.Should().ThrowAsync<FederatedAccountLinkRequiredException>();
        _createdUsers.Should().HaveCount(createdBefore, "no second account for the same federated human");
        _upsertedLinks.Should().HaveCount(upsertedBefore, "no silent merge into the first provider's account");
    }

    // ── (4) Same provider, different subject → still a conflict, not an extra link ────

    [Fact]
    public async Task GitHubSignIn_SameProviderDifferentSubject_SameEmail_RequiresExplicitLink()
    {
        ExistingFederatedAccount("github", "github-node-OTHER");
        var sut = CreateSut();

        var act = () => sut.GitHubCallbackSignInAsync(GitHubRequest());

        var exception = (await act.Should().ThrowAsync<FederatedAccountLinkRequiredException>())
            .Which;
        exception.Provider.Should().Be("github");
        exception.Message.Should().Contain("link", "the error must tell the user how to proceed");

        _upsertedLinks.Should().BeEmpty("a second subject must not be silently attached to the account");
    }

    // ── (5) Regression pin: verified email may still auto-link to a LOCAL account ─────

    [Fact]
    public async Task GitHubSignIn_EmailOwnedByLocalPasswordAccount_StillAutoLinks()
    {
        var localUser = User.CreateWithPassword(SharedEmail, "local", BCrypt.Net.BCrypt.HashPassword("irrelevant"));
        _userRepoMock
            .Setup(x => x.GetByEmailAsync(SharedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localUser);
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(localUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalLogin>());

        var result = await CreateSut().GitHubCallbackSignInAsync(GitHubRequest());

        result.Success.Should().BeTrue("the documented verified-email auto-link for local accounts is unchanged");
        result.UserId.Should().Be(localUser.Id);
        _createdUsers.Should().BeEmpty();
        _upsertedLinks.Should().ContainSingle(link => link.UserId == localUser.Id && link.Provider == "github");
    }

    // ── (6) The provider+subject link itself still wins: returning user signs in ──────

    [Fact]
    public async Task GitHubSignIn_ExistingProviderSubjectLink_ReusedWithoutChallenge()
    {
        var existing = ExistingFederatedAccount("github", "github-node-1");
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("github", "github-node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = existing.Id,
                Provider = "github",
                ProviderKey = "github-node-1"
            });

        var result = await CreateSut().GitHubCallbackSignInAsync(GitHubRequest());

        result.Success.Should().BeTrue("resolution by (provider, providerKey) is the primary path and never challenges");
        _createdUsers.Should().BeEmpty();
    }
}
