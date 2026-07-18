using FluentAssertions;
using GameGuild.CQRS;


using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class RecordSubscriptionPaymentCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly RecordSubscriptionPaymentCommandHandler _handler;

    public RecordSubscriptionPaymentCommandHandlerTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _handler = new RecordSubscriptionPaymentCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldRecordPayment_WhenSubscriptionExists()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new RecordSubscriptionPaymentCommand(
            subscriptionId,
            Amount: 29.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "pay_12345",
            ForBillingCycle: 1
        );

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlreadyProcessed_WhenSameIdempotencyKey()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var idempotencyKey = "pay_12345";
        
        // First payment
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, idempotencyKey, forBillingCycle: 1);

        var command = new RecordSubscriptionPaymentCommand(
            subscriptionId,
            Amount: 29.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: idempotencyKey,
            ForBillingCycle: 1
        );

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsAlreadyProcessed.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRecordPayment_WithBillingCycle()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new RecordSubscriptionPaymentCommand(
            subscriptionId,
            Amount: 29.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "pay_67890",
            ForBillingCycle: 1
        );

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command = new RecordSubscriptionPaymentCommand(
            subscriptionId,
            Amount: 29.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "pay_12345",
            ForBillingCycle: 1
        );

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
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new RecordSubscriptionPaymentCommand(
            subscriptionId,
            Amount: 49.99m,
            Currency: "EUR",
            PaymentDate: new DateTime(2026, 1, 15),
            IdempotencyKey: "pay_abc123",
            ForBillingCycle: 3
        );

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.Amount.Should().Be(49.99m);
        command.Currency.Should().Be("EUR");
        command.PaymentDate.Should().Be(new DateTime(2026, 1, 15));
        command.IdempotencyKey.Should().Be("pay_abc123");
        command.ForBillingCycle.Should().Be(3);
    }

    [Fact]
    public void Command_ShouldDefaultToInvalidBillingCycleIdentity()
    {
        // Arrange & Act
        var command = new RecordSubscriptionPaymentCommand(
            Guid.NewGuid(),
            Amount: 29.99m,
            Currency: "USD",
            PaymentDate: DateTime.UtcNow,
            IdempotencyKey: "pay_12345"
        );

        // Assert
        command.ForBillingCycle.Should().Be(0);
    }

    private static Subscription CreateActiveSubscription(Guid id)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);
        subscription.Activate();

        return subscription;
    }
}
