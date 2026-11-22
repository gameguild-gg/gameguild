using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpChallengeTimestampPrecisionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 10)]
    public async Task CreationAndReturnedExpiryUseThePersistableMicrosecondBoundary(int clockTicks, int expectedTicks)
    {
        var wholeSecond = new DateTimeOffset(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);
        var actor = new ActorContextAccessor();
        actor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(Guid.NewGuid())
            .WithAttribute("session_id", Guid.NewGuid().ToString()).Build());
        StepUpChallenge? captured = null;
        var store = new Mock<IStepUpChallengeStore>();
        store.Setup(x => x.AddAsync(It.IsAny<StepUpChallenge>(), It.IsAny<CancellationToken>()))
            .Callback((StepUpChallenge challenge, CancellationToken _) => captured = challenge).Returns(Task.CompletedTask);
        var service = new StepUpReceiptService(actor, store.Object, Mock.Of<IMfaService>(),
            Mock.Of<IWebAuthnAuthenticationService>(), new FixedClock(wholeSecond.AddTicks(clockTicks)));
        var response = await service.CreateChallengeAsync(SessionMutationCommandHandler.TrustDeviceStepUpBinding("Synthetic device"));
        var stored = Assert.IsType<StepUpChallenge>(captured);
        Assert.Equal(wholeSecond.AddTicks(expectedTicks), stored.CreatedAt);
        Assert.Equal(wholeSecond.AddMinutes(5).AddTicks(expectedTicks), stored.ExpiresAt);
        Assert.Equal(stored.ExpiresAt, response.ExpiresAt);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
