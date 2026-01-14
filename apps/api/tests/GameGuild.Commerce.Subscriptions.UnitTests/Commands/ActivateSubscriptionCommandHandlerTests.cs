using GameGuild.ValueObjects;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Commerce.Subscriptions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ActivateSubscriptionCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly ActivateSubscriptionCommandHandler _handler;

    public ActivateSubscriptionCommandHandlerTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _handler = new ActivateSubscriptionCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldActivateSubscription_WhenSubscriptionExists()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreatePendingSubscription(subscriptionId);
        var command = new ActivateSubscriptionCommand(subscriptionId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        _mockRepository.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command = new ActivateSubscriptionCommand(subscriptionId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscription not found");
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent_WhenSubscriptionActivated()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreatePendingSubscription(subscriptionId);
        var command = new ActivateSubscriptionCommand(subscriptionId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var events = subscription.DomainEvents;
        events.Should().Contain(e => e.GetType().Name == "SubscriptionActivatedEvent");
    }

    private static Subscription CreatePendingSubscription(Guid id)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );

        // Use reflection to set the Id
        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);

        return subscription;
    }
}
