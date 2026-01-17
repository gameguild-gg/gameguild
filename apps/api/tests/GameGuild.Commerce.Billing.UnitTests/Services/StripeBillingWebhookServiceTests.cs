using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class StripeBillingWebhookServiceTests
{
    [Fact]
    public async Task ProcessStripeWebhookAsync_Should_Return_AlreadyProcessed_For_Duplicate()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("evt_1", PaymentProviders.Stripe, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingWebhookEvent { ExternalEventId = "evt_1", Provider = PaymentProviders.Stripe, ProcessedAt = DateTime.UtcNow });

        var service = CreateService(repository, Mock.Of<ISubscriptionQueryService>(), Mock.Of<ISubscriptionBillingService>(), Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessStripeWebhookAsync("evt_1", "invoice.payment_succeeded", "{}", "sig", CancellationToken.None);

        result.WasAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_Should_Activate_Subscription_On_Status_Change()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("evt_2", PaymentProviders.Stripe, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var queryService = new Mock<ISubscriptionQueryService>();
        queryService
            .Setup(q => q.GetByExternalIdAsync("sub_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var lifecycle = new Mock<ISubscriptionLifecycleService>();

        var service = CreateService(repository, queryService.Object, Mock.Of<ISubscriptionBillingService>(), lifecycle.Object, Mock.Of<ISubscriptionExternalIdService>());

        var payload = "{\"id\":\"evt_2\",\"type\":\"customer.subscription.updated\",\"data\":{\"object\":{\"id\":\"sub_123\",\"status\":\"active\"}}}";

        var result = await service.ProcessStripeWebhookAsync("evt_2", "customer.subscription.updated", payload, "sig", CancellationToken.None);

        result.Processed.Should().BeTrue();
        lifecycle.Verify(l => l.ActivateAsync(subscription.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_Should_Record_Payment_On_Payment_Succeeded()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("evt_3", PaymentProviders.Stripe, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var queryService = new Mock<ISubscriptionQueryService>();
        queryService
            .Setup(q => q.GetByExternalIdAsync("sub_456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var billingService = new Mock<ISubscriptionBillingService>();

        var service = CreateService(repository, queryService.Object, billingService.Object, Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var payload = "{\"id\":\"evt_3\",\"type\":\"invoice.payment_succeeded\",\"data\":{\"object\":{\"subscription\":\"sub_456\",\"amount_paid\":5000,\"currency\":\"usd\"}}}";

        var result = await service.ProcessStripeWebhookAsync("evt_3", "invoice.payment_succeeded", payload, "sig", CancellationToken.None);

        result.Processed.Should().BeTrue();
        billingService.Verify(b => b.RecordPaymentAsync(subscription.Id, 50m, "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static StripeBillingWebhookService CreateService(
        Mock<IBillingWebhookRepository> repository,
        ISubscriptionQueryService queryService,
        ISubscriptionBillingService billingService,
        ISubscriptionLifecycleService lifecycleService,
        ISubscriptionExternalIdService externalIdService)
    {
        return new StripeBillingWebhookService(
            repository.Object,
            NullLogger<StripeBillingWebhookService>.Instance,
            lifecycleService,
            queryService,
            billingService,
            externalIdService);
    }
}