using System.Threading;
using FluentAssertions;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public class ExternalLoginHandlersTests
{
    private readonly Mock<IExternalLoginRepository> _externalLoginRepoMock = new();
    private readonly Mock<IGoogleIdTokenVerifier> _googleVerifierMock = new();
    private readonly Mock<IOAuthService> _oauthServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();

    public ExternalLoginHandlersTests()
    {
        _googleVerifierMock
            .Setup(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifiedGoogleUser
            {
                Sub = "google-sub-1",
                Email = "user@example.com",
                EmailVerified = true,
                Name = "Test User"
            });

        _externalLoginRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin dto, CancellationToken _) => dto);

        _userRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.CreateWithPassword("user@example.com", "User", "password-hash"));
    }

    // ── List ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetExternalLogins_ReturnsDtos_InRepositoryOrder_NewestFirst()
    {
        var userId = Guid.NewGuid();
        var older = new ExternalLogin { UserId = userId, Provider = "google", ProviderKey = "sub-1", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var newer = new ExternalLogin { UserId = userId, Provider = "discord", ProviderKey = "snow-1", CreatedAt = DateTime.UtcNow };
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([newer, older]);

        var result = await new GetExternalLoginsQueryHandler(_externalLoginRepoMock.Object)
            .Handle(new GetExternalLoginsQuery { UserId = userId }, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Provider.Should().Be("discord");
        result[0].CreatedAt.Should().Be(newer.CreatedAt);
        result[1].Provider.Should().Be("google");
        result[1].CreatedAt.Should().Be(older.CreatedAt);
    }

    // ── Google link ─────────────────────────────────────────────────────

    [Fact]
    public async Task LinkGoogle_ValidToken_NoExistingLink_WritesRow()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("google", "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);

        var handler = new LinkGoogleAccountCommandHandler(_googleVerifierMock.Object, _externalLoginRepoMock.Object);
        await handler.Handle(new LinkGoogleAccountCommand { UserId = userId, IdToken = "valid-id-token" }, CancellationToken.None);

        _googleVerifierMock.Verify(x => x.VerifyAsync("valid-id-token", It.IsAny<CancellationToken>()), Times.Once);
        _externalLoginRepoMock.Verify(
            x => x.AddAsync(
                It.Is<ExternalLogin>(e => e.UserId == userId && e.Provider == "google" && e.ProviderKey == "google-sub-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkGoogle_AlreadyLinkedToSameUser_IsIdempotent_NoSecondWrite()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("google", "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin { UserId = userId, Provider = "google", ProviderKey = "google-sub-1" });

        var handler = new LinkGoogleAccountCommandHandler(_googleVerifierMock.Object, _externalLoginRepoMock.Object);
        await handler.Handle(new LinkGoogleAccountCommand { UserId = userId, IdToken = "valid-id-token" }, CancellationToken.None);

        _externalLoginRepoMock.Verify(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkGoogle_LinkedToAnotherUser_ThrowsConflict_NeverUpserts()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("google", "google-sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin { UserId = Guid.NewGuid(), Provider = "google", ProviderKey = "google-sub-1" });

        var handler = new LinkGoogleAccountCommandHandler(_googleVerifierMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkGoogleAccountCommand { UserId = userId, IdToken = "valid-id-token" }, CancellationToken.None);

        await act.Should().ThrowAsync<ExternalLoginConflictException>()
            .WithMessage("Social account already linked to another user");
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
        _externalLoginRepoMock.Verify(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkGoogle_InvalidToken_ThrowsUnauthorized_NoRepoAccess()
    {
        _googleVerifierMock
            .Setup(x => x.VerifyAsync("forged-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Google ID token is invalid"));

        var handler = new LinkGoogleAccountCommandHandler(_googleVerifierMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkGoogleAccountCommand { UserId = Guid.NewGuid(), IdToken = "forged-token" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Google ID token is invalid");
        _externalLoginRepoMock.Verify(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Discord link-authorize ──────────────────────────────────────────

    [Fact]
    public async Task DiscordLinkAuthorize_ReturnsAuthUrl_AndPerRequestState()
    {
        string? capturedState = null;
        _oauthServiceMock
            .Setup(x => x.GetAuthorizationUrlAsync("discord", "https://app/callback", It.IsAny<string>(), null))
            .Callback((string _, string _, string state, string[]? _) => capturedState = state)
            .ReturnsAsync("https://discord.com/oauth2/authorize?client_id=abc");

        var handler = new DiscordLinkAuthorizeCommandHandler(_oauthServiceMock.Object, NullLogger<DiscordLinkAuthorizeCommandHandler>.Instance);
        var result = await handler.Handle(
            new DiscordLinkAuthorizeCommand { RedirectUri = "https://app/callback" }, CancellationToken.None);

        result.AuthUrl.Should().Be("https://discord.com/oauth2/authorize?client_id=abc");
        result.State.Should().MatchRegex("^[0-9a-f]{32}$");
        result.State.Should().Be(capturedState);
    }

    // ── Discord link-callback ───────────────────────────────────────────

    [Fact]
    public async Task LinkDiscord_CallbackExchangesCodeForProfile_AndWritesRow()
    {
        var userId = Guid.NewGuid();
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", "auth-code", "state-1", "https://app/callback"))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord", Email = "d@example.com", EmailVerified = true });
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        await handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = userId,
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        _externalLoginRepoMock.Verify(
            x => x.AddAsync(
                It.Is<ExternalLogin>(e => e.UserId == userId && e.Provider == "discord" && e.ProviderKey == "2516582401"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     F2 defect pin (identity-transfer race): when the pre-check misses but another user's
    ///     row commits concurrently, the link MUST go through the insert-only path. The old code
    ///     called UpsertAsync here — its internal read found the winner and silently reassigned
    ///     ownership via the update branch. Insert-only AddAsync cannot reassign: it either
    ///     inserts or throws DbUpdateException on the unique index.
    /// </summary>
    [Fact]
    public async Task LinkDiscord_PreCheckMiss_InsertOnlyPath_NeverCallsUpsert()
    {
        var userId = Guid.NewGuid();
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord" });
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        await handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = userId,
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        _externalLoginRepoMock.Verify(
            x => x.AddAsync(
                It.Is<ExternalLogin>(e => e.UserId == userId && e.Provider == "discord" && e.ProviderKey == "2516582401"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkDiscord_Race_DbUpdateExceptionFromAddThenForeignRow_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord" });
        _externalLoginRepoMock
            .SetupSequence(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null)
            .ReturnsAsync(new ExternalLogin { UserId = Guid.NewGuid(), Provider = "discord", ProviderKey = "2516582401" });
        _externalLoginRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint"));

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = userId,
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ExternalLoginConflictException>()
            .WithMessage("Social account already linked to another user");
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkDiscord_Race_DbUpdateExceptionFromAddThenSameUserRow_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord" });
        _externalLoginRepoMock
            .SetupSequence(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null)
            .ReturnsAsync(new ExternalLogin { UserId = userId, Provider = "discord", ProviderKey = "2516582401" });
        _externalLoginRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint"));

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = userId,
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkDiscord_Race_DbUpdateExceptionFromAddThenRefetchNull_Rethrows()
    {
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord" });
        _externalLoginRepoMock
            .SetupSequence(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null)
            .ReturnsAsync((ExternalLogin?)null);
        _externalLoginRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint"));

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = Guid.NewGuid(),
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task LinkDiscord_PreCheckForeignOwner_ThrowsConflict_NeverUpserts()
    {
        var userId = Guid.NewGuid();
        _oauthServiceMock
            .Setup(x => x.HandleCallbackAsync("discord", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OAuthUserProfile { ProviderId = "2516582401", Provider = "Discord" });
        _externalLoginRepoMock
            .Setup(x => x.GetByProviderKeyAsync("discord", "2516582401", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalLogin { UserId = Guid.NewGuid(), Provider = "discord", ProviderKey = "2516582401" });

        var handler = new LinkDiscordAccountCommandHandler(_oauthServiceMock.Object, _externalLoginRepoMock.Object);
        var act = () => handler.Handle(new LinkDiscordAccountCommand
        {
            UserId = userId,
            Code = "auth-code",
            State = "state-1",
            RedirectUri = "https://app/callback"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ExternalLoginConflictException>();
        _externalLoginRepoMock.Verify(x => x.UpsertAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
        _externalLoginRepoMock.Verify(x => x.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Unlink ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Unlink_LinkedWithPasswordAndSecondProvider_DeletesRow()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ExternalLogin { UserId = userId, Provider = "discord", ProviderKey = "snow-1" },
                new ExternalLogin { UserId = userId, Provider = "google", ProviderKey = "sub-1" }
            ]);
        _externalLoginRepoMock
            .Setup(x => x.DeleteAsync("discord", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new UnlinkExternalLoginCommandHandler(_externalLoginRepoMock.Object, _userRepoMock.Object);
        await handler.Handle(new UnlinkExternalLoginCommand { UserId = userId, Provider = "discord" }, CancellationToken.None);

        _externalLoginRepoMock.Verify(x => x.DeleteAsync("discord", userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unlink_LastMethodWithNoPassword_ThrowsGuard_NeverDeletes()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalLogin { UserId = userId, Provider = "google", ProviderKey = "sub-1" }]);
        _userRepoMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.CreateOAuthUser("user@example.com", "OAuth User"));

        var handler = new UnlinkExternalLoginCommandHandler(_externalLoginRepoMock.Object, _userRepoMock.Object);
        var act = () => handler.Handle(new UnlinkExternalLoginCommand { UserId = userId, Provider = "google" }, CancellationToken.None);

        await act.Should().ThrowAsync<LastSignInMethodException>()
            .WithMessage("Cannot remove the last sign-in method");
        _externalLoginRepoMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unlink_LastMethodButPasswordSet_DeletesRow()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExternalLogin { UserId = userId, Provider = "google", ProviderKey = "sub-1" }]);

        var handler = new UnlinkExternalLoginCommandHandler(_externalLoginRepoMock.Object, _userRepoMock.Object);
        await handler.Handle(new UnlinkExternalLoginCommand { UserId = userId, Provider = "google" }, CancellationToken.None);

        _externalLoginRepoMock.Verify(x => x.DeleteAsync("google", userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unlink_NotLinked_ThrowsNotFound_NeverDeletes()
    {
        var userId = Guid.NewGuid();
        _externalLoginRepoMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new UnlinkExternalLoginCommandHandler(_externalLoginRepoMock.Object, _userRepoMock.Object);
        var act = () => handler.Handle(new UnlinkExternalLoginCommand { UserId = userId, Provider = "google" }, CancellationToken.None);

        await act.Should().ThrowAsync<ExternalLoginNotFoundException>();
        _externalLoginRepoMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
