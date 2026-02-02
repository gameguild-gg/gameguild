using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class ApplePayBillingWebhookServiceTests
{
    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Return_Failed_When_Validation_Fails()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Failed("bad"));

        var service = CreateService(repository, validator.Object, Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bad");
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Return_AlreadyProcessed_For_Duplicate()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingWebhookEvent { ExternalEventId = "tx", Provider = PaymentProviders.AppleAppStore, ProcessedAt = DateTime.UtcNow });

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("SUBSCRIBED", null, "tx", "orig", "prod", null, "Sandbox"));

        var service = CreateService(repository, validator.Object, Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.WasAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Create_Subscription_On_Subscribed()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("SUBSCRIBED", null, "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var lifecycle = new Mock<ISubscriptionLifecycleService>();
        lifecycle
            .Setup(l => l.CreateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<BillingCycle>(),
                It.IsAny<Money>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var externalIdService = new Mock<ISubscriptionExternalIdService>();

        var service = CreateService(repository, validator.Object, lifecycle.Object, externalIdService.Object);

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        externalIdService.Verify(s => s.SetExternalIdsAsync(subscription.Id, "orig", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Record_Payment_On_Renewed()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("DID_RENEW", null, "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var query = new Mock<ISubscriptionQueryService>();
        query.Setup(q => q.GetByExternalIdAsync("orig", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var billing = new Mock<ISubscriptionBillingService>();

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            query.Object,
            billing.Object,
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        billing.Verify(b => b.RecordPaymentAsync(subscription.Id, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Record_Failure_On_Failed_To_Renew()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("DID_FAIL_TO_RENEW", "BILLING_RETRY", "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var query = new Mock<ISubscriptionQueryService>();
        query.Setup(q => q.GetByExternalIdAsync("orig", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var billing = new Mock<ISubscriptionBillingService>();

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            query.Object,
            billing.Object,
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        billing.Verify(b => b.RecordPaymentFailureAsync(subscription.Id, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Cancel_On_Expired()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("EXPIRED", null, "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var query = new Mock<ISubscriptionQueryService>();
        query.Setup(q => q.GetByExternalIdAsync("orig", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var lifecycle = new Mock<ISubscriptionLifecycleService>();

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            lifecycle.Object,
            query.Object,
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        lifecycle.Verify(l => l.CancelAsync(subscription.Id, CancellationReason.Custom, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Cancel_On_GracePeriodExpired()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("GRACE_PERIOD_EXPIRED", null, "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var query = new Mock<ISubscriptionQueryService>();
        query.Setup(q => q.GetByExternalIdAsync("orig", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var lifecycle = new Mock<ISubscriptionLifecycleService>();

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            lifecycle.Object,
            query.Object,
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
        lifecycle.Verify(l => l.CancelAsync(subscription.Id, CancellationReason.Custom, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Handle_AutoRenew_Disabled()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("DID_CHANGE_RENEWAL_STATUS", "AUTO_RENEW_DISABLED", "tx", "orig", "prod", null, "Sandbox"));

        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, new Money(10m, "USD"), DateTime.UtcNow);
        var query = new Mock<ISubscriptionQueryService>();
        query.Setup(q => q.GetByExternalIdAsync("orig", It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            query.Object,
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAppStoreNotificationAsync_Should_Handle_Refund_Event()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success("REFUND", null, "tx", "orig", "prod", null, "Sandbox"));

        var service = new ApplePayBillingWebhookService(
            repository.Object,
            validator.Object,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessAppStoreNotificationAsync("payload", CancellationToken.None);

        result.Processed.Should().BeTrue();
    }

    private static ApplePayBillingWebhookService CreateService(
        Mock<IBillingWebhookRepository> repository,
        IApplePayReceiptValidationService validator,
        ISubscriptionLifecycleService lifecycle,
        ISubscriptionExternalIdService externalIdService)
    {
        return new ApplePayBillingWebhookService(
            repository.Object,
            validator,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            lifecycle,
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            externalIdService);
    }
}