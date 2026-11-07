using GameGuild.CQRS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SmsMfaContinuationBindingTests
{
    [Fact]
    public async Task VerifiedEnrollmentCreatesMissingConfigurationWithoutTotpProvisioning()
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(port => port.GetByUserIdAsync(fixture.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((UserMfaConfiguration?)null);
        UserMfaConfiguration? created = null;
        fixture.Repository.Setup(port => port.CreateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<UserMfaConfiguration, CancellationToken>((configuration, _) => created = configuration)
            .ReturnsAsync((UserMfaConfiguration configuration, CancellationToken _) => configuration);
        var service = fixture.Create();
        Assert.True((await service.StartEnrollmentAsync(fixture.UserId, "+15555550123")).Success);
        Assert.NotNull(fixture.Code);
        Assert.True((await service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
        Assert.NotNull(created);
        Assert.Equal(fixture.UserId, created.UserId);
        Assert.True(created.IsEnabled);
        Assert.True(created.IsSetupComplete);
        Assert.Equal(MfaMethod.Sms, created.PreferredMethod);
        Assert.Null(created.TotpSecretKey);
        Assert.NotNull(created.EnabledAt);
        fixture.Repository.Verify(port => port.CreateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(port => port.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BoundProofRejectsOtherContinuationsAndPurposesWithoutLosingTheOriginal()
    {
        using var fixture = new Fixture();
        var continuation = Guid.NewGuid();
        var service = fixture.Create();
        Assert.True((await service.SendSignInChallengeAsync(fixture.UserId, continuation, CancellationToken.None)).Success);
        Assert.NotNull(fixture.Code);
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, Guid.NewGuid(), fixture.Code, CancellationToken.None));
        Assert.False(await service.VerifyCodeAsync(fixture.UserId, fixture.Code));
        Assert.False((await service.CompleteEnrollmentAsync(fixture.UserId, fixture.Code)).Success);
        Assert.True(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
    }

    [Fact]
    public async Task LegacyUnboundProofCannotCompleteASignInContinuation()
    {
        using var fixture = new Fixture();
        var service = fixture.Create();
        Assert.True((await service.SendChallengeAsync(fixture.UserId)).Success);
        Assert.NotNull(fixture.Code);
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, Guid.NewGuid(), fixture.Code, CancellationToken.None));
        Assert.True(await service.VerifyCodeAsync(fixture.UserId, fixture.Code));
    }

    [Fact]
    public async Task ParallelResendsAcrossScopedInstancesDispatchExactlyOnce()
    {
        using var fixture = new Fixture();
        var responses = await Task.WhenAll(Enumerable.Range(0, 24).Select(_ => Task.Run(() =>
            fixture.Create().SendSignInChallengeAsync(fixture.UserId, Guid.NewGuid(), CancellationToken.None))));
        Assert.Single(responses, response => response.Success);
        Assert.Equal(1, fixture.Deliveries);
    }

    [Fact]
    public async Task ParallelProofConsumptionAcrossScopedInstancesSucceedsExactlyOnce()
    {
        using var fixture = new Fixture();
        var continuation = Guid.NewGuid();
        Assert.True((await fixture.Create().SendSignInChallengeAsync(fixture.UserId, continuation, CancellationToken.None)).Success);
        Assert.NotNull(fixture.Code);
        var results = await Task.WhenAll(Enumerable.Range(0, 24).Select(_ => Task.Run(() =>
            fixture.Create().VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None))));
        Assert.Single(results, success => success);
        fixture.Repository.Verify(port => port.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockoutDeniesVerificationBeforeConsumingTheIssuedProof()
    {
        using var fixture = new Fixture();
        var continuation = Guid.NewGuid();
        var service = fixture.Create();
        Assert.True((await service.SendSignInChallengeAsync(fixture.UserId, continuation, CancellationToken.None)).Success);
        Assert.NotNull(fixture.Code);
        fixture.Configuration.LockedOutUntil = DateTime.UtcNow.AddMinutes(5);
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
        fixture.Configuration.LockedOutUntil = null;
        Assert.True(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("incomplete")]
    [InlineData("no-phone")]
    public async Task UnenrolledStateCannotDispatchOrVerifyBoundProof(string fault)
    {
        using var fixture = new Fixture();
        if (fault == "disabled") { fixture.Configuration.IsEnabled = false; }
        if (fault == "incomplete") { fixture.Configuration.IsSetupComplete = false; }
        if (fault == "no-phone") { fixture.Configuration.SmsPhoneNumber = null; }
        var service = fixture.Create();
        Assert.False((await service.SendSignInChallengeAsync(fixture.UserId, Guid.NewGuid(), CancellationToken.None)).Success);
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, Guid.NewGuid(), "invalid", CancellationToken.None));
        Assert.Equal(0, fixture.Deliveries);
        fixture.Repository.Verify(port => port.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Tracking.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FailedProofInvokesAccountBudgetAndSuccessResetsTheRecordedFailures()
    {
        using var fixture = new Fixture();
        fixture.Tracking.Setup(port => port.RecordFailedMfaAttemptAsync(fixture.Configuration, MfaMethod.Sms,
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback(() => fixture.Configuration.FailedAttempts++)
            .Returns(Task.CompletedTask);
        var continuation = Guid.NewGuid();
        var service = fixture.Create();
        Assert.True((await service.SendSignInChallengeAsync(fixture.UserId, continuation, CancellationToken.None)).Success);
        Assert.NotNull(fixture.Code);
        var wrong = (fixture.Code[0] == '0' ? "1" : "0") + fixture.Code[1..];
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, continuation, wrong, CancellationToken.None));
        Assert.Equal(1, fixture.Configuration.FailedAttempts);
        Assert.True(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
        Assert.Equal(0, fixture.Configuration.FailedAttempts);
        Assert.NotNull(fixture.Configuration.LastUsedAt);
        fixture.Tracking.Verify(port => port.RecordMfaAttemptAsync(fixture.UserId, MfaMethod.Sms, true, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelledDeliveryCannotLeaveAUsableBoundProof()
    {
        using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Publisher.Setup(port => port.Publish(It.IsAny<SmsMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<SmsMfaCodeRequestedNotification, CancellationToken>((notice, _) => fixture.Code = notice.Code)
            .Returns((SmsMfaCodeRequestedNotification _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(token);
            });
        var continuation = Guid.NewGuid();
        var service = fixture.Create();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SendSignInChallengeAsync(fixture.UserId, continuation, cancellation.Token));
        Assert.NotNull(fixture.Code);
        Assert.False(await service.VerifySignInCodeAsync(fixture.UserId, continuation, fixture.Code, CancellationToken.None));
        fixture.Repository.Verify(port => port.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions());
        private int deliveries;
        public Guid UserId { get; } = Guid.NewGuid();
        public UserMfaConfiguration Configuration { get; }
        public Mock<IUserMfaConfigurationRepository> Repository { get; } = new(MockBehavior.Strict);
        public Mock<IMfaAttemptTrackingService> Tracking { get; } = new(MockBehavior.Strict);
        public Mock<IPublisher> Publisher { get; } = new(MockBehavior.Strict);
        public string? Code { get; set; }
        public int Deliveries => Volatile.Read(ref deliveries);

        public Fixture()
        {
            Configuration = new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = UserId,
                IsEnabled = true, IsSetupComplete = true, SmsPhoneNumber = "+15555550123" };
            Repository.Setup(port => port.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Configuration);
            Repository.Setup(port => port.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserMfaConfiguration value, CancellationToken _) => value);
            Tracking.Setup(port => port.IsLockedOut(Configuration)).Returns(false);
            Tracking.Setup(port => port.RecordFailedMfaAttemptAsync(Configuration, MfaMethod.Sms, It.IsAny<string>(), null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Tracking.Setup(port => port.RecordMfaAttemptAsync(UserId, MfaMethod.Sms, true, null, null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Publisher.Setup(port => port.Publish(It.IsAny<SmsMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
                .Callback<SmsMfaCodeRequestedNotification, CancellationToken>((notice, _) =>
                {
                    Code = notice.Code;
                    Interlocked.Increment(ref deliveries);
                }).Returns(Task.CompletedTask);
        }

        public SmsMfaService Create() => new(NullLogger<SmsMfaService>.Instance, cache, Repository.Object, Publisher.Object, Tracking.Object);
        public void Dispose() => cache.Dispose();
    }
}
