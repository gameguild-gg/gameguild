using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class AuthAttemptServiceSuccessPathTests
{
    [Fact]
    public async Task RecordSuccessfulAttemptAsync_ShouldPersistSuccessfulAttempt()
    {
        var repository = new Mock<IAuthenticationAttemptRepository>();
        AuthenticationAttempt? captured = null;

        repository
            .Setup(x => x.CreateAsync(It.IsAny<AuthenticationAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<AuthenticationAttempt, CancellationToken>((attempt, _) => captured = attempt)
            .ReturnsAsync((AuthenticationAttempt attempt, CancellationToken _) => attempt);

        var sut = new AuthAttemptService(
            repository.Object,
            Mock.Of<IUserEnumerationProtectionService>(),
            NullLogger<AuthAttemptService>.Instance);

        var userId = Guid.NewGuid();

        await sut.RecordSuccessfulAttemptAsync(
            "user@example.com",
            userId,
            "198.51.100.1",
            "UnitTestAgent",
            TimeSpan.FromMilliseconds(42));

        captured.Should().NotBeNull();
        captured!.Email.Should().Be("user@example.com");
        captured.UserId.Should().Be(userId);
        captured.IpAddress.Should().Be("198.51.100.1");
        captured.UserAgent.Should().Be("UnitTestAgent");
        captured.IsSuccessful.Should().BeTrue();
        captured.FailureReason.Should().BeNull();
        captured.ProcessingTime.Should().Be(TimeSpan.FromMilliseconds(42));
        captured.AttemptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        repository.Verify(x => x.CreateAsync(It.IsAny<AuthenticationAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_ShouldPersistFailureAndRecordEnumerationAttempt()
    {
        var repository = new Mock<IAuthenticationAttemptRepository>();
        var enumerationProtection = new Mock<IUserEnumerationProtectionService>();
        AuthenticationAttempt? captured = null;

        repository
            .Setup(x => x.CreateAsync(It.IsAny<AuthenticationAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<AuthenticationAttempt, CancellationToken>((attempt, _) => captured = attempt)
            .ReturnsAsync((AuthenticationAttempt attempt, CancellationToken _) => attempt);

        var sut = new AuthAttemptService(
            repository.Object,
            enumerationProtection.Object,
            NullLogger<AuthAttemptService>.Instance);

        await sut.RecordFailedAttemptAsync(
            "user@example.com",
            Guid.NewGuid(),
            "203.0.113.9",
            "UnitTestAgent",
            "InvalidCredentials",
            TimeSpan.FromMilliseconds(75));

        captured.Should().NotBeNull();
        captured!.IsSuccessful.Should().BeFalse();
        captured.FailureReason.Should().Be("InvalidCredentials");
        captured.IpAddress.Should().Be("203.0.113.9");
        captured.UserAgent.Should().Be("UnitTestAgent");
        captured.ProcessingTime.Should().Be(TimeSpan.FromMilliseconds(75));
        captured.AttemptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        enumerationProtection.Verify(x => x.RecordEnumerationAttemptAsync("203.0.113.9", "login"), Times.Once);
    }
}

public sealed class RevokeTokenHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUseFallbackIpAndReturnUnitValue()
    {
        var authService = new Mock<IAuthService>();
        var handler = new RevokeTokenHandler(authService.Object, NullLogger<RevokeTokenHandler>.Instance);
        var command = new RevokeTokenCommand
        {
            RefreshToken = "refresh-token",
            IpAddress = null
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        authService.Verify(x => x.RevokeRefreshTokenAsync("refresh-token", "Unknown", It.IsAny<CancellationToken>()), Times.Once);
    }
}