using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class EmailMfaLifecycleRegressionTests : IDisposable
{
    private readonly Guid userId = Guid.NewGuid();
    private readonly MemoryCache cache = new(new MemoryCacheOptions());
    private readonly Mock<IUserMfaConfigurationRepository> configurations = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IPublisher> publisher = new();
    private UserMfaConfiguration? configuration;
    private string? deliveredCode;

    public EmailMfaLifecycleRegressionTests()
    {
        configurations.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => configuration);
        configurations.Setup(r => r.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMfaConfiguration value, CancellationToken _) => value);
        users.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = "member@example.com" });
        publisher.Setup(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMfaCodeRequestedNotification, CancellationToken>((message, _) => deliveredCode = message.Code)
            .Returns(Task.CompletedTask);
    }

    public void Dispose() => cache.Dispose();

    private EmailMfaService CreateService() =>
        new(NullLogger<EmailMfaService>.Instance, cache, configurations.Object, users.Object, publisher.Object);

    private UserMfaConfiguration Configure(bool enabled, string? address = "member@example.com") =>
        configuration = new UserMfaConfiguration { UserId = userId, IsEnabled = enabled, EmailMfaAddress = address };

    [Theory]
    [InlineData(false, "member@example.com")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    public async Task Challenge_RequiresEnabledFactorAndEnrolledAddress(bool enabled, string? address)
    {
        Configure(enabled, address);

        var result = await CreateService().SendChallengeAsync(userId);

        result.Success.Should().BeFalse();
        deliveredCode.Should().BeNull();
        publisher.Verify(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Verification_RejectsIssuedCodeWhenFactorIsDisabledOrConfigurationRemoved(bool removeConfiguration)
    {
        Configure(true);
        var service = CreateService();
        (await service.SendChallengeAsync(userId)).Success.Should().BeTrue();
        deliveredCode.Should().NotBeNull();
        if (removeConfiguration) { configuration = null; }
        else { configuration!.IsEnabled = false; }

        (await service.VerifyCodeAsync(userId, deliveredCode!)).Should().BeFalse();
    }

    [Fact]
    public async Task Enrollment_DeniedSignInVerificationDoesNotDiscardPendingSetup()
    {
        Configure(false, null);
        var service = CreateService();
        (await service.StartEnrollmentAsync(userId)).Success.Should().BeTrue();

        (await service.VerifyCodeAsync(userId, deliveredCode!)).Should().BeFalse();
        (await service.CompleteEnrollmentAsync(userId, deliveredCode!)).Success.Should().BeTrue();
    }

    [Fact]
    public async Task Enrollment_RecordsCompletedSetupAfterValidCode()
    {
        var state = Configure(false, null);
        var service = CreateService();
        (await service.StartEnrollmentAsync(userId)).Success.Should().BeTrue();
        state.IsEnabled.Should().BeFalse();
        state.IsSetupComplete.Should().BeFalse();

        var result = await service.CompleteEnrollmentAsync(userId, deliveredCode!);

        result.Success.Should().BeTrue();
        state.IsEnabled.Should().BeTrue();
        state.IsSetupComplete.Should().BeTrue();
        state.EmailMfaAddress.Should().Be("member@example.com");
        configurations.Verify(r => r.UpdateAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Disable_RemovesEmailFactorAndCompletedSetupAndRevokesTokens()
    {
        var state = Configure(true);
        state.IsSetupComplete = true;
        var revocation = new Mock<ITokenRevocationService>();
        var service = new MfaAttemptTrackingService(
            NullLogger<MfaAttemptTrackingService>.Instance, configurations.Object,
            Mock.Of<IMfaAttemptRepository>(), new HttpContextAccessor(), revocation.Object);

        (await service.DisableMfaAsync(userId)).Should().BeTrue();

        state.IsEnabled.Should().BeFalse();
        state.EmailMfaAddress.Should().BeNull();
        state.IsSetupComplete.Should().BeFalse();
        configurations.Verify(r => r.UpdateAsync(state, It.IsAny<CancellationToken>()), Times.Once);
        revocation.Verify(r => r.RevokeAllUserTokensAsync(userId, "MFA disabled", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnabledLegacyEnrollment_RemainsUsableAndChallengeCodeIsSingleUse()
    {
        var state = Configure(true);
        state.IsSetupComplete.Should().BeFalse();
        var service = CreateService();

        (await service.SendChallengeAsync(userId)).Success.Should().BeTrue();
        (await service.VerifyCodeAsync(userId, deliveredCode!)).Should().BeTrue();
        (await service.VerifyCodeAsync(userId, deliveredCode!)).Should().BeFalse();
    }
}
