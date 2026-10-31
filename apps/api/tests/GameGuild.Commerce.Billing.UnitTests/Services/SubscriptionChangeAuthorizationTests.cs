using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public sealed class SubscriptionChangeAuthorizationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, true, false)]
    public async Task Handler_OnlyDelegatesForAnAuthenticatedOwnerOrTenantAdministrator(
        bool authenticated, bool owner, bool sameTenant, bool admin)
    {
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var actorId = owner ? ownerId : Guid.NewGuid();
        var subscription = new Subscription(tenantId, Guid.NewGuid(), ownerId,
            BillingCycle.Monthly, new Money(100m, "USD"), DateTime.UtcNow);
        var repository = new Mock<ISubscriptionRepository>();
        repository.Setup(value => value.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous with
        {
            IsAuthenticated = authenticated,
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = sameTenant ? tenantId : Guid.NewGuid(),
            Roles = admin ? new HashSet<string> { "TenantAdmin" } : new HashSet<string>()
        });
        var saga = new Mock<ISubscriptionChangeSaga>();
        var handler = new OrchestrateSubscriptionPlanChangeCommandHandler(saga.Object, accessor.Object, repository.Object);
        var command = new OrchestrateSubscriptionPlanChangeCommand(subscription.Id, Guid.NewGuid());
        var allowed = authenticated && sameTenant && (owner || admin);

        if (allowed)
        {
            await handler.Handle(command, CancellationToken.None);
            saga.Verify(value => value.ExecuteAsync(It.Is<SubscriptionChangeSagaRequest>(request => request.ActorId == actorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            saga.Verify(value => value.ExecuteAsync(It.IsAny<SubscriptionChangeSagaRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
