using GameGuild.ValueObjects;
using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class CancelSubscriptionCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly CancelSubscriptionCommandHandler _handler;

    public CancelSubscriptionCommandHandlerTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _handler = new CancelSubscriptionCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldCancelSubscription_WhenSubscriptionExists()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new CancelSubscriptionCommand(subscriptionId, CancellationReason.UserRequested, "User requested cancellation", DateTime.UtcNow.AddDays(7));

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
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        _mockRepository.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command = new CancelSubscriptionCommand(subscriptionId, CancellationReason.UserRequested, null, DateTime.UtcNow);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GameGuild.SharedKernel.SubscriptionNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationDetails_ToSubscriptionEntity()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var reason = CancellationReason.PaymentFailed;
        var note = "Payment method expired";
        var effectiveDate = DateTime.UtcNow.AddDays(30);
        var command = new CancelSubscriptionCommand(subscriptionId, reason, note, effectiveDate);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        subscription.CancellationReason.Should().Be(reason);
        subscription.CancellationNote.Should().Be(note);
        subscription.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static Subscription CreateActiveSubscription(Guid id)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow.AddDays(-30),
            trialEndDate: null
        );

        // Use reflection to set the Id
        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);

        subscription.Activate();
        return subscription;
    }
}
