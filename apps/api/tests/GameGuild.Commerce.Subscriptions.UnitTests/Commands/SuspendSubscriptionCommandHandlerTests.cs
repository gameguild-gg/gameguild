using FluentAssertions;
using GameGuild.CQRS;


using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class SuspendSubscriptionCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly SuspendSubscriptionCommandHandler _handler;

    public SuspendSubscriptionCommandHandlerTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _handler = new SuspendSubscriptionCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldSuspendSubscription_WhenSubscriptionIsActive()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new SuspendSubscriptionCommand(subscriptionId, "Payment failed");

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
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        _mockRepository.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSuspendSubscription_WithNullReason()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new SuspendSubscriptionCommand(subscriptionId, null);

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
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command = new SuspendSubscriptionCommand(subscriptionId, "Test");

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent_WhenSuspended()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new SuspendSubscriptionCommand(subscriptionId, "Test reason");

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType().Name == "SubscriptionSuspendedEvent");
    }

    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var reason = "Payment declined";
        var command = new SuspendSubscriptionCommand(subscriptionId, reason);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.Reason.Should().Be(reason);
    }

    [Fact]
    public void Command_ShouldHaveDefaultNullReason()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new SuspendSubscriptionCommand(subscriptionId);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.Reason.Should().BeNull();
    }

    private static Subscription CreateActiveSubscription(Guid id)
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

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);
        subscription.Activate();

        return subscription;
    }
}
