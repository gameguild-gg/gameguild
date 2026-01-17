using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public class PayPalBillingWebhookServiceTests
{
    [Fact]
    public async Task ProcessPayPalWebhookAsync_Should_Return_AlreadyProcessed_For_Duplicate()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.PayPal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingWebhookEvent { ExternalEventId = "tx", Provider = PaymentProviders.PayPal, ProcessedAt = DateTime.UtcNow });

        var service = CreateService(repository, Mock.Of<IPayPalSignatureVerificationService>(), Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessPayPalWebhookAsync("wh", "{}", "tx", "time", "sig", null, null, CancellationToken.None);

        result.WasAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessPayPalWebhookAsync_Should_Return_Failed_When_Signature_Invalid()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.PayPal, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var verification = new Mock<IPayPalSignatureVerificationService>();
        verification
            .Setup(v => v.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PayPalVerificationResult.Failed("bad"));

        var service = CreateService(repository, verification.Object, Mock.Of<ISubscriptionLifecycleService>(), Mock.Of<ISubscriptionExternalIdService>());

        var result = await service.ProcessPayPalWebhookAsync("wh", "{}", "tx", "time", "sig", null, null, CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bad");
    }

    [Fact]
    public async Task ProcessPayPalWebhookAsync_Should_Create_Subscription_For_Subscription_Created()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx", PaymentProviders.PayPal, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var verification = new Mock<IPayPalSignatureVerificationService>();
        verification
            .Setup(v => v.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PayPalVerificationResult.Success());

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

        var service = CreateService(repository, verification.Object, lifecycle.Object, externalIdService.Object);

        var payload = "{\"event_type\":\"BILLING.SUBSCRIPTION.CREATED\",\"resource\":{\"id\":\"sub_123\",\"status\":\"ACTIVE\",\"amount\":{\"total\":\"10.00\",\"currency\":\"USD\"}}}";
        var result = await service.ProcessPayPalWebhookAsync("wh", payload, "tx", "time", "sig", null, null, CancellationToken.None);

        result.Processed.Should().BeTrue();
        externalIdService.Verify(s => s.SetExternalIdsAsync(subscription.Id, "sub_123", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PayPalBillingWebhookService CreateService(
        Mock<IBillingWebhookRepository> repository,
        IPayPalSignatureVerificationService verification,
        ISubscriptionLifecycleService lifecycle,
        ISubscriptionExternalIdService externalIdService)
    {
        return new PayPalBillingWebhookService(
            repository.Object,
            verification,
            NullLogger<PayPalBillingWebhookService>.Instance,
            lifecycle,
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            externalIdService);
    }
}