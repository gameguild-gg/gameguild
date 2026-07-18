using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

public sealed class StripeWebhookIngressSecurityTests
{
    [Fact]
    public async Task ProcessStripeWebhookAsync_RejectsForgedSignatureBeforePersisting()
    {
        var repository = new Mock<IBillingWebhookRepository>(MockBehavior.Strict);
        var verifier = new Mock<IStripeWebhookVerifier>(MockBehavior.Strict);
        verifier.Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidWebhookSignatureException("forged"));
        var service = CreateService(repository.Object, verifier.Object);

        var act = () => service.ProcessStripeWebhookAsync(
            "{\"id\":\"evt_forged\",\"type\":\"invoice.payment_succeeded\",\"data\":{\"object\":{\"id\":\"in_1\"}}}",
            "t=1,v1=forged",
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookSignatureException>();
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_DoesNotConvertInboxPersistenceFailureIntoAcknowledgement()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(candidate => candidate.GetByProviderScopeAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "we_test",
                "evt_database_failure",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(candidate => candidate.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        repository
            .Setup(candidate => candidate.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent candidate, CancellationToken _) => candidate);

        var verifier = new Mock<IStripeWebhookVerifier>();
        verifier.Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(CreateVerifiedEvent("evt_database_failure", "unhandled.event"));
        var service = CreateService(repository.Object, verifier.Object);

        var result = await service.ProcessStripeWebhookAsync(
            "{\"id\":\"evt_database_failure\",\"type\":\"unhandled.event\",\"data\":{\"object\":{\"id\":\"obj_1\"}}}",
            "t=1,v1=forged",
            CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.RequiresRetry.Should().BeTrue();
        repository.Verify(
            candidate => candidate.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_RejectsUnknownSubscriptionBeforeInboxPersistence()
    {
        var repository = new Mock<IBillingWebhookRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetByProviderScopeAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "we_test",
                "evt_unknown_subscription",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        var verifier = new Mock<IStripeWebhookVerifier>();
        verifier.Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(CreateVerifiedEvent(
                "evt_unknown_subscription",
                "invoice.payment_succeeded",
                Guid.NewGuid(),
                10m,
                "USD"));
        var queryService = new Mock<ISubscriptionQueryService>();
        queryService.Setup(candidate => candidate.GetByExternalIdAsync("sub_contract", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        var service = CreateService(repository.Object, verifier.Object, queryService.Object);

        var act = () => service.ProcessStripeWebhookAsync("{}", "signature", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>()
            .WithMessage("*subscription*");
        repository.Verify(candidate => candidate.GetByProviderScopeAsync(
            PaymentProviders.Stripe,
            "test",
            "platform",
            "we_test",
            "evt_unknown_subscription",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_RejectsTenantAmountAndCurrencyMismatchBeforePersistence()
    {
        var localTenantId = Guid.NewGuid();
        var subscription = new Subscription(
            localTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            new Money(10m, "USD"),
            DateTime.UtcNow);
        var queryService = new Mock<ISubscriptionQueryService>();
        queryService.Setup(candidate => candidate.GetByExternalIdAsync("sub_contract", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        foreach (var invalidEvent in new[]
                 {
                     CreateVerifiedEvent("evt_tenant", "invoice.payment_succeeded", Guid.NewGuid(), 10m, "USD"),
                     CreateVerifiedEvent("evt_amount", "invoice.payment_succeeded", localTenantId, 9m, "USD"),
                     CreateVerifiedEvent("evt_currency", "invoice.payment_succeeded", localTenantId, 10m, "EUR")
                 })
        {
            var repository = new Mock<IBillingWebhookRepository>(MockBehavior.Strict);
            repository.Setup(candidate => candidate.GetByProviderScopeAsync(
                    PaymentProviders.Stripe,
                    "test",
                    "platform",
                    "we_test",
                    invalidEvent.EventId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((BillingWebhookEvent?)null);
            var verifier = new Mock<IStripeWebhookVerifier>();
            verifier.Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(invalidEvent);
            var service = CreateService(repository.Object, verifier.Object, queryService.Object);

            var act = () => service.ProcessStripeWebhookAsync("{}", "signature", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidWebhookPayloadException>();
            repository.Verify(candidate => candidate.GetByProviderScopeAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "we_test",
                invalidEvent.EventId,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ProcessStripeWebhookAsync_BindsMonetaryEventBeforeDurableAcceptance()
    {
        var tenantId = Guid.NewGuid();
        BillingWebhookEvent? acceptedEvent = null;
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(candidate => candidate.GetByProviderScopeAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "we_test",
                "evt_payment_binding",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(candidate => candidate.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent candidate, CancellationToken _) =>
            {
                acceptedEvent = candidate;
                return candidate;
            });
        repository
            .Setup(candidate => candidate.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent candidate, CancellationToken _) => candidate);

        var verifiedEvent = CreateVerifiedEvent("evt_payment_binding", "payment_intent.succeeded", tenantId, 100m, "USD") with
        {
            ProviderObjectId = "pi_bound",
            ProviderObjectType = "payment_intent",
            ProviderMonetaryLeg = "capture"
        };
        var verifier = new Mock<IStripeWebhookVerifier>();
        verifier.Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(verifiedEvent);
        var bindingValidator = new Mock<IStripeProviderObjectBindingValidator>(MockBehavior.Strict);
        bindingValidator
            .Setup(candidate => candidate.ValidateAsync(verifiedEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeWebhookPaymentBinding(Guid.NewGuid(), tenantId));
        var service = CreateService(
            repository.Object,
            verifier.Object,
            providerObjectBindingValidator: bindingValidator.Object);

        var result = await service.ProcessStripeWebhookAsync("{}", "signature", CancellationToken.None);

        result.Processed.Should().BeTrue();
        acceptedEvent.Should().NotBeNull();
        acceptedEvent!.TenantId.Should().Be(tenantId);
        acceptedEvent.ProviderObjectId.Should().Be("pi_bound");
        bindingValidator.VerifyAll();
    }

    private static StripeBillingWebhookService CreateService(
        IBillingWebhookRepository repository,
        IStripeWebhookVerifier verifier,
        ISubscriptionQueryService? queryService = null,
        IStripeProviderObjectBindingValidator? providerObjectBindingValidator = null)
    {
        return new StripeBillingWebhookService(
            repository,
            verifier,
            providerObjectBindingValidator ?? CreateNoOpProviderObjectBindingValidator(),
            NullLogger<StripeBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            queryService ?? Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());
    }

    private static IStripeProviderObjectBindingValidator CreateNoOpProviderObjectBindingValidator()
    {
        var validator = new Mock<IStripeProviderObjectBindingValidator>();
        validator
            .Setup(candidate => candidate.ValidateAsync(
                It.IsAny<VerifiedStripeWebhookEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StripeWebhookPaymentBinding?)null);
        return validator.Object;
    }

    private static VerifiedStripeWebhookEvent CreateVerifiedEvent(
        string eventId,
        string eventType,
        Guid? tenantId = null,
        decimal? amount = null,
        string? currency = null) => new()
    {
        EventId = eventId,
        EventType = eventType,
        ProviderEnvironment = "test",
        ProviderAccountId = "platform",
        WebhookEndpointId = "we_test",
        EventSchemaVersion = "2023-10-16",
        ProviderObjectId = "obj_1",
        ProviderObjectType = "test_object",
        ProviderMonetaryLeg = "nonmonetary",
        TenantId = tenantId,
        ExternalSubscriptionId = eventType.StartsWith("invoice.", StringComparison.Ordinal)
            ? "sub_contract"
            : null,
        Amount = amount,
        Currency = currency,
        VerifiedPayload = "{\"data\":{\"object\":{\"id\":\"obj_1\"}}}",
        RetainedPayload = "{}",
        PayloadSha256 = new string('0', 64)
    };
}
