using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public sealed class PaymentSubscriptionSyncServiceTests
{
    private readonly Mock<ISubscriptionRepository> _repository = new();
    private readonly PaymentSubscriptionSyncService _service;

    public PaymentSubscriptionSyncServiceTests()
    {
        _service = new PaymentSubscriptionSyncService(
            _repository.Object,
            Mock.Of<ILogger<PaymentSubscriptionSyncService>>());
    }

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_ShouldSkip_WhenPaymentHasNoSubscription()
    {
        await _service.SyncSuccessfulPaymentAsync(
            Guid.NewGuid(),
            subscriptionId: null,
            amount: 29.99m,
            currency: "USD",
            processedAt: DateTime.UtcNow);

        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_ShouldSkip_WhenSubscriptionIsMissing()
    {
        var subscriptionId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        await _service.SyncSuccessfulPaymentAsync(
            Guid.NewGuid(),
            subscriptionId,
            29.99m,
            "USD",
            DateTime.UtcNow);

        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_ShouldRecordPayment_WhenSubscriptionExists()
    {
        var paymentId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(Guid.NewGuid());
        var processedAt = DateTime.UtcNow;

        _repository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _repository
            .Setup(repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _service.SyncSuccessfulPaymentAsync(
            paymentId,
            subscription.Id,
            29.99m,
            "USD",
            processedAt);

        subscription.LastPaymentIdempotencyKey.Should().Be($"payment:{paymentId}");
        subscription.LastPaymentAt.Should().Be(processedAt);
        _repository.Verify(repository => repository.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_ShouldNotUpdateAgain_WhenPaymentWasAlreadyProcessed()
    {
        var paymentId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(Guid.NewGuid());
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, $"payment:{paymentId}");

        _repository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _service.SyncSuccessfulPaymentAsync(
            paymentId,
            subscription.Id,
            29.99m,
            "USD",
            DateTime.UtcNow.AddMinutes(5));

        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
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
            trialEndDate: null);

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);
        subscription.Activate();

        return subscription;
    }
}
