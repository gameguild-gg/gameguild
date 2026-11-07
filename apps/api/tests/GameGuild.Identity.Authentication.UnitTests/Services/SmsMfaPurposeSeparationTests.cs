using GameGuild.CQRS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SmsMfaPurposeSeparationTests
{
    [Fact]
    public async Task EnrollmentCodeCannotBeConsumedBySignInVerification()
    {
        using var fixture = new Fixture();
        Assert.True((await fixture.Service.StartEnrollmentAsync(fixture.UserId, "+15555550123")).Success);
        Assert.NotNull(fixture.Code);
        Assert.False(await fixture.Service.VerifyCodeAsync(fixture.UserId, fixture.Code));
        Assert.Equal("+15555550999", fixture.Configuration.SmsPhoneNumber);
        Assert.True((await fixture.Service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
        Assert.Equal("+15555550123", fixture.Configuration.SmsPhoneNumber);
    }

    [Fact]
    public async Task SignInCodeCannotBeConsumedByEnrollmentCompletion()
    {
        using var fixture = new Fixture();
        Assert.True((await fixture.Service.SendChallengeAsync(fixture.UserId)).Success);
        Assert.NotNull(fixture.Code);
        Assert.False((await fixture.Service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
        fixture.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await fixture.Service.VerifyCodeAsync(fixture.UserId, fixture.Code));
    }

    [Fact]
    public async Task CorrectEnrollmentPurposeStillUpdatesVerifiedDestination()
    {
        using var fixture = new Fixture();
        Assert.True((await fixture.Service.StartEnrollmentAsync(fixture.UserId, "+15555550123")).Success);
        Assert.NotNull(fixture.Code);
        Assert.True((await fixture.Service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
        Assert.Equal("+15555550123", fixture.Configuration.SmsPhoneNumber);
        fixture.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False((await fixture.Service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
    }

    [Fact]
    public async Task CorrectChallengePurposeRejectsWrongCodeAndIsSingleUse()
    {
        using var fixture = new Fixture();
        Assert.True((await fixture.Service.SendChallengeAsync(fixture.UserId)).Success);
        Assert.NotNull(fixture.Code);
        var wrongCode = (fixture.Code[0] == '0' ? "1" : "0") + fixture.Code[1..];
        Assert.False(await fixture.Service.VerifyCodeAsync(fixture.UserId, wrongCode));
        Assert.True(await fixture.Service.VerifyCodeAsync(fixture.UserId, fixture.Code));
        Assert.False(await fixture.Service.VerifyCodeAsync(fixture.UserId, fixture.Code));
        fixture.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions());
        public Guid UserId { get; } = Guid.NewGuid();
        public Mock<IUserMfaConfigurationRepository> Repository { get; } = new(MockBehavior.Strict);
        public UserMfaConfiguration Configuration { get; }
        public SmsMfaService Service { get; }
        public string? Code { get; private set; }

        public Fixture()
        {
            Configuration = new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = UserId,
                IsEnabled = true, IsSetupComplete = true, SmsPhoneNumber = "+15555550999" };
            Repository.Setup(repository => repository.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Configuration);
            Repository.Setup(repository => repository.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserMfaConfiguration configuration, CancellationToken _) => configuration);
            var publisher = new Mock<IPublisher>(MockBehavior.Strict);
            publisher.Setup(port => port.Publish(It.IsAny<SmsMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
                .Callback<SmsMfaCodeRequestedNotification, CancellationToken>((notification, _) => Code = notification.Code)
                .Returns(Task.CompletedTask);
            Service = new SmsMfaService(NullLogger<SmsMfaService>.Instance, cache, Repository.Object, publisher.Object, Mock.Of<IMfaAttemptTrackingService>());
        }

        public void Dispose() => cache.Dispose();
    }
}
