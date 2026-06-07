using FluentAssertions;
using MockQueryable.Moq;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class PatchSubscriptionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenSubscriptionNotFound()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(Array.Empty<Subscription>().AsQueryable().BuildMockDbSet().Object);

        var handler = new PatchSubscriptionHandler(context.Object);
        var command = new PatchSubscriptionCommand(Guid.NewGuid(), BillingCycle.Annually);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{command.SubscriptionId}*not found*");
    }

    [Fact]
    public async Task Handle_ShouldLeaveSubscriptionUnchanged_WhenNoPatchFieldsProvided()
    {
        var subscription = CreateActiveSubscription();
        var context = CreateContext(subscription);
        var handler = new PatchSubscriptionHandler(context.Object);
        var command = new PatchSubscriptionCommand(subscription.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        subscription.BillingCycle.Should().Be(BillingCycle.Monthly);
        subscription.AutoRenew.Should().BeTrue();
        subscription.ExternalId.Should().Be("sub_existing");
        subscription.ExternalCustomerId.Should().Be("cus_existing");
        subscription.Metadata.Should().BeNull();
        context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotChangeBillingCycle_WhenSameCycleProvided()
    {
        var subscription = CreateActiveSubscription();
        var context = CreateContext(subscription);
        var handler = new PatchSubscriptionHandler(context.Object);
        var command = new PatchSubscriptionCommand(subscription.Id, BillingCycle: BillingCycle.Monthly);

        await handler.Handle(command, CancellationToken.None);

        subscription.BillingCycle.Should().Be(BillingCycle.Monthly);
        subscription.Amount.Amount.Should().Be(29.99m);
    }

    [Fact]
    public async Task Handle_ShouldApplyAllProvidedUpdates_WhenPatchContainsNewValues()
    {
        var subscription = CreateActiveSubscription();
        var context = CreateContext(subscription);
        var handler = new PatchSubscriptionHandler(context.Object);
        var command = new PatchSubscriptionCommand(
            subscription.Id,
            BillingCycle: BillingCycle.Annually,
            AutoRenew: false,
            ExternalSubscriptionId: "sub_new",
            ExternalCustomerId: "cus_new",
            Metadata: "{\"tier\":\"enterprise\"}");

        await handler.Handle(command, CancellationToken.None);

        subscription.BillingCycle.Should().Be(BillingCycle.Annually);
        subscription.AutoRenew.Should().BeFalse();
        subscription.ExternalId.Should().Be("sub_new");
        subscription.ExternalCustomerId.Should().Be("cus_new");
        subscription.Metadata.Should().Be("{\"tier\":\"enterprise\"}");
    }

    [Fact]
    public async Task Handle_ShouldFallbackToExistingExternalIds_WhenOnlyOneIdentifierIsProvided()
    {
        var subscription = CreateActiveSubscription();
        var context = CreateContext(subscription);
        var handler = new PatchSubscriptionHandler(context.Object);

        await handler.Handle(
            new PatchSubscriptionCommand(subscription.Id, ExternalCustomerId: "cus_replaced"),
            CancellationToken.None);

        subscription.ExternalId.Should().Be("sub_existing");
        subscription.ExternalCustomerId.Should().Be("cus_replaced");

        await handler.Handle(
            new PatchSubscriptionCommand(subscription.Id, ExternalSubscriptionId: "sub_replaced"),
            CancellationToken.None);

        subscription.ExternalId.Should().Be("sub_replaced");
        subscription.ExternalCustomerId.Should().Be("cus_replaced");
    }

    private static Mock<IApplicationDbContext> CreateContext(Subscription subscription)
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(c => c.Set<Subscription>()).Returns(new[] { subscription }.AsQueryable().BuildMockDbSet().Object);
        context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return context;
    }

    private static Subscription CreateActiveSubscription()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null);

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, Guid.NewGuid());
        subscription.Activate();
        subscription.SetExternalIds("sub_existing", "cus_existing");
        return subscription;
    }
}
