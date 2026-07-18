using FluentAssertions;
using GameGuild.Commerce.Payments;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Security;

public sealed class StripeProviderObjectBindingValidatorTests
{
    [Fact]
    public async Task ValidateAsync_RejectsUnknownMonetaryProviderObject()
    {
        var repository = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var verifiedEvent = CreateEvent();
        repository
            .Setup(x => x.GetByProviderMappingAsync(
                PaymentProviders.Stripe,
                verifiedEvent.ProviderEnvironment,
                verifiedEvent.ProviderAccountId,
                verifiedEvent.ProviderObjectId,
                verifiedEvent.ProviderObjectType,
                verifiedEvent.ProviderMonetaryLeg,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var validator = new StripeProviderObjectBindingValidator(repository.Object);

        var act = () => validator.ValidateAsync(verifiedEvent, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>()
            .WithMessage("*unknown payment provider object*");
    }

    [Fact]
    public async Task ValidateAsync_AcceptsExactImmutableProviderMapping()
    {
        var tenantId = Guid.NewGuid();
        var payment = CreateBoundPayment(tenantId);
        var repository = CreateRepositoryReturning(payment);
        var validator = new StripeProviderObjectBindingValidator(repository.Object);

        var result = await validator.ValidateAsync(CreateEvent(tenantId), CancellationToken.None);

        result.Should().Be(new StripeWebhookPaymentBinding(payment.Id, tenantId));
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("amount")]
    [InlineData("currency")]
    public async Task ValidateAsync_RejectsLocalFinancialObjectMismatch(string mismatch)
    {
        var tenantId = Guid.NewGuid();
        var payment = CreateBoundPayment(tenantId);
        var repository = CreateRepositoryReturning(payment);
        var validator = new StripeProviderObjectBindingValidator(repository.Object);
        var verifiedEvent = CreateEvent(
            mismatch == "tenant" ? Guid.NewGuid() : tenantId,
            amount: mismatch == "amount" ? 99m : 100m,
            currency: mismatch == "currency" ? "EUR" : "USD");

        var act = () => validator.ValidateAsync(verifiedEvent, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>()
            .WithMessage($"*{mismatch}*");
    }

    [Fact]
    public async Task ValidateAsync_RejectsCumulativeRefundAndDisputeAboveConfirmedAmount()
    {
        var tenantId = Guid.NewGuid();
        var payment = CreateBoundPayment(tenantId);
        var repository = CreateRepositoryReturning(payment);
        var validator = new StripeProviderObjectBindingValidator(repository.Object);
        var verifiedEvent = CreateEvent(tenantId) with
        {
            CumulativeRefundedAmount = 60m,
            CumulativeDisputedAmount = 50m
        };

        var act = () => validator.ValidateAsync(verifiedEvent, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>()
            .WithMessage("*cumulative provider amounts*");
    }

    [Fact]
    public async Task ValidateAsync_RejectsRegressingCumulativeRefundTotal()
    {
        var tenantId = Guid.NewGuid();
        var payment = CreateBoundPayment(tenantId);
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("pi_bound");
        payment.ProcessRefund(20m, "re_local", "partial");
        var repository = CreateRepositoryReturning(payment);
        var validator = new StripeProviderObjectBindingValidator(repository.Object);
        var verifiedEvent = CreateEvent(tenantId) with { CumulativeRefundedAmount = 10m };

        var act = () => validator.ValidateAsync(verifiedEvent, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>()
            .WithMessage("*refund total regressed*");
    }

    [Theory]
    [InlineData("nonmonetary")]
    [InlineData("subscription")]
    public async Task ValidateAsync_SkipsEventsWithoutPaymentValueMovement(string monetaryLeg)
    {
        var repository = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var validator = new StripeProviderObjectBindingValidator(repository.Object);

        var result = await validator.ValidateAsync(
            CreateEvent() with { ProviderMonetaryLeg = monetaryLeg },
            CancellationToken.None);

        result.Should().BeNull();
        repository.VerifyNoOtherCalls();
    }

    private static Mock<IPaymentRepository> CreateRepositoryReturning(Payment payment)
    {
        var repository = new Mock<IPaymentRepository>(MockBehavior.Strict);
        repository
            .Setup(x => x.GetByProviderMappingAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "pi_bound",
                "payment_intent",
                "capture",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        return repository;
    }

    private static Payment CreateBoundPayment(Guid tenantId)
    {
        var payment = Payment.Create(tenantId, 100m, "USD", $"idem-{Guid.NewGuid():N}");
        payment.BindProviderMapping(
            PaymentProviders.Stripe,
            "test",
            "platform",
            "pi_bound",
            "payment_intent",
            "capture");
        return payment;
    }

    private static VerifiedStripeWebhookEvent CreateEvent(
        Guid? tenantId = null,
        decimal amount = 100m,
        string currency = "USD") => new()
    {
        EventId = "evt_bound",
        EventType = "payment_intent.succeeded",
        ProviderEnvironment = "test",
        ProviderAccountId = "platform",
        WebhookEndpointId = "we_platform",
        EventSchemaVersion = "2023-10-16",
        ProviderObjectId = "pi_bound",
        ProviderObjectType = "payment_intent",
        ProviderMonetaryLeg = "capture",
        VerifiedPayload = "{}",
        RetainedPayload = "{}",
        PayloadSha256 = "sha256",
        TenantId = tenantId,
        Amount = amount,
        Currency = currency
    };
}
