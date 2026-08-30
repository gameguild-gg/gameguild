using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.Economy.AdRewards;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public sealed class EconomyAdRewardsControllerContractTests
{
    [Fact]
    public void SelfServiceBodiesExposeOnlyBusinessIntent()
    {
        typeof(StartMyAdRewardSessionRequest).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo([
                "Network",
                "CreativeId",
                "DeviceRiskHash",
                "IpRiskHash",
                "AsnRiskHash",
                "RequiredDurationSeconds",
                "IdempotencyKey"
            ]);
        typeof(CompleteMyAdRewardSessionRequest).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo(["Token", "Playback", "ProviderProof", "IdempotencyKey"]);
        typeof(EconomyAdRewardsController).GetMethods()
            .Should().NotContain(method => method.Name.Contains("ConfirmDeferred", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(EconomyProtectedOperationState.Denied, 403)]
    [InlineData(EconomyProtectedOperationState.ReviewRequired, 409)]
    [InlineData(EconomyProtectedOperationState.Hold, 409)]
    [InlineData(EconomyProtectedOperationState.ComplianceUnavailable, 503)]
    public async Task ProtectedOperationStatesAreReturnedAsStructuredResponses(
        EconomyProtectedOperationState state,
        int expectedStatus)
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var actor = new ActorContextAccessor();
        actor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        Guid? reviewId = state == EconomyProtectedOperationState.ReviewRequired ? Guid.NewGuid() : null;
        var completions = new Mock<IDurableAdRewardCompletionService>(MockBehavior.Strict);
        completions.Setup(service => service.CompleteAsync(
                It.IsAny<CompleteDurableAdRewardSessionRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException<DurableAdRewardCompletionResult>(
                new EconomyProtectedOperationException(state, reviewId, ["not-ready"])));
        var controller = new EconomyAdRewardsController(
            Mock.Of<IDurableAdRewardSessionService>(),
            completions.Object,
            Mock.Of<IDurableAdRewardSessionReader>(),
            Mock.Of<IEconomyWalletDirectory>(),
            actor,
            TimeProvider.System);

        var result = await controller.Complete(
            Guid.NewGuid(),
            new CompleteMyAdRewardSessionRequest(
                "signed-token",
                new AdPlaybackEvidence(
                    DateTimeOffset.UtcNow.AddSeconds(-30),
                    DateTimeOffset.UtcNow,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.Zero,
                    [0, 25, 50, 75, 100]),
                null,
                "completion-1"),
            CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(expectedStatus);
        response.Value.Should().BeEquivalentTo(new
        {
            State = state,
            ReviewId = reviewId,
            Diagnostics = new[] { "not-ready" }
        });
    }
}
