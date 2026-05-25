using FluentAssertions;
using GameGuild.Commerce.Payments;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public class PaymentSubscriptionSyncServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ISubscriptionBillingService> _billingService = new();
    private readonly Mock<ISubscriptionLifecycleService> _lifecycleService = new();
    private readonly Mock<ILogger<PaymentSubscriptionSyncService>> _logger = new();

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_WhenSubscriptionIsPendingActivation_ShouldRecordPaymentAndActivate()
    {
        var subscriptionId = Guid.NewGuid();
        var processedAt = DateTime.UtcNow;
        var subscription = new Subscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            new Money(25m, "USD"),
            processedAt);

        subscription.Status.Should().Be(SubscriptionStatus.PendingActivation);

        _subscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _billingService
            .Setup(x => x.RecordPaymentAsync(subscription.Id, 25m, "USD", processedAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var service = new PaymentSubscriptionSyncService(
            _subscriptionRepository.Object,
            _billingService.Object,
            _lifecycleService.Object,
            _logger.Object);

        await service.SyncSuccessfulPaymentAsync(
            "payment-1",
            subscriptionId,
            25m,
            "USD",
            processedAt,
            CancellationToken.None);

        _billingService.Verify(
            x => x.RecordPaymentAsync(subscription.Id, 25m, "USD", processedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _lifecycleService.Verify(x => x.ActivateAsync(subscription.Id, It.IsAny<CancellationToken>()), Times.Once);
        _lifecycleService.Verify(x => x.ReactivateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncSuccessfulPaymentAsync_WhenSubscriptionIsSuspended_ShouldRecordPaymentAndReactivate()
    {
        var subscriptionId = Guid.NewGuid();
        var processedAt = DateTime.UtcNow;
        var subscription = new Subscription(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            new Money(25m, "USD"),
            processedAt);

        subscription.Activate();
        subscription.Suspend("billing retry");
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);

        _subscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _billingService
            .Setup(x => x.RecordPaymentAsync(subscription.Id, 25m, "USD", processedAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var service = new PaymentSubscriptionSyncService(
            _subscriptionRepository.Object,
            _billingService.Object,
            _lifecycleService.Object,
            _logger.Object);

        await service.SyncSuccessfulPaymentAsync(
            "payment-2",
            subscriptionId,
            25m,
            "USD",
            processedAt,
            CancellationToken.None);

        _billingService.Verify(
            x => x.RecordPaymentAsync(subscription.Id, 25m, "USD", processedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _lifecycleService.Verify(x => x.ReactivateAsync(subscription.Id, It.IsAny<CancellationToken>()), Times.Once);
        _lifecycleService.Verify(x => x.ActivateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
