using FluentAssertions;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Password change/reset revocation battery tests (issue #278): a credential change must
///     revoke every refresh token, terminate every session through the audited session
///     service with the PasswordChanged reason, record the lifecycle event and persist the
///     versioned token cutoff covering legacy access tokens. Cutoff-store failures propagate.
/// </summary>
public sealed class PasswordChangeRevocationServiceTests
{
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IRefreshTokenRepository> refreshTokenRepository = new();
    private readonly Mock<ISessionManagementService> sessionManagementService = new();
    private readonly Mock<IVersionedUserTokenRevocationService> tokenRevocationService = new();
    private readonly Mock<IRefreshTokenLifecycleRecorder> lifecycleRecorder = new();

    private PasswordChangeRevocationService CreateService() =>
        new(
            userRepository.Object,
            refreshTokenRepository.Object,
            sessionManagementService.Object,
            tokenRevocationService.Object,
            lifecycleRecorder.Object,
            NullLogger<PasswordChangeRevocationService>.Instance);

    [Fact]
    public async Task RevokeAll_RevokesRefreshTokensTerminatesSessionsAndRecordsVersionedCutoff()
    {
        var userId = Guid.NewGuid();
        refreshTokenRepository
            .Setup(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagementService
            .Setup(s => s.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        userRepository
            .Setup(r => r.GetTokenVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        tokenRevocationService
            .Setup(t => t.RevokeAllUserTokensAsync(userId, 5, "Password changed", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var terminated = await CreateService().RevokeAllSessionsAndTokensAsync(userId, "Password changed");

        terminated.Should().Be(3);
        refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()), Times.Once);
        sessionManagementService.Verify(
            s => s.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, null, It.IsAny<CancellationToken>()),
            Times.Once);
        lifecycleRecorder.Verify(
            r => r.RecordMutationAsync(
                It.Is<RefreshTokenLifecycleEvent>(e =>
                    e.Operation == RefreshTokenLifecycleOperation.AllRevoked && e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        tokenRevocationService.Verify(t => t.RevokeAllUserTokensAsync(userId, 5, "Password changed", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAll_WithZeroActiveSessions_StillRevokesTokensAndRecordsCutoff()
    {
        var userId = Guid.NewGuid();
        refreshTokenRepository
            .Setup(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagementService
            .Setup(s => s.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        userRepository
            .Setup(r => r.GetTokenVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        tokenRevocationService
            .Setup(t => t.RevokeAllUserTokensAsync(userId, 2, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var terminated = await CreateService().RevokeAllSessionsAndTokensAsync(userId, "Password reset");

        terminated.Should().Be(0);
        refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()), Times.Once);
        tokenRevocationService.Verify(t => t.RevokeAllUserTokensAsync(userId, 2, "Password reset", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAll_MissingTokenVersion_FailsClosedWithZeroCutoff()
    {
        var userId = Guid.NewGuid();
        refreshTokenRepository
            .Setup(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagementService
            .Setup(s => s.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        // The user vanished concurrently: no persisted version to anchor the cutoff.
        userRepository
            .Setup(r => r.GetTokenVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        await CreateService().RevokeAllSessionsAndTokensAsync(userId, "Password changed");

        // Minimum version 0 revokes every previously issued token — fail closed.
        tokenRevocationService.Verify(t => t.RevokeAllUserTokensAsync(userId, 0, "Password changed", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAll_CutoffStoreFailure_Propagates()
    {
        var userId = Guid.NewGuid();
        refreshTokenRepository
            .Setup(r => r.RevokeAllForUserAsync(userId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sessionManagementService
            .Setup(s => s.TerminateAllUserSessionsAsync(
                userId, SessionTerminationReason.PasswordChanged, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        userRepository
            .Setup(r => r.GetTokenVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        tokenRevocationService
            .Setup(t => t.RevokeAllUserTokensAsync(userId, 5, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("revocation store unavailable"));

        var act = () => CreateService().RevokeAllSessionsAndTokensAsync(userId, "Password changed");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RevokeAll_BlankReason_IsRejected(string? reason)
    {
        var act = () => CreateService().RevokeAllSessionsAndTokensAsync(Guid.NewGuid(), reason!);

        await act.Should().ThrowAsync<ArgumentException>();
        refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
