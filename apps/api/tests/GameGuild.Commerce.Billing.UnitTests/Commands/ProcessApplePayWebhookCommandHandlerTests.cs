using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

public class ProcessApplePayWebhookCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Failed_When_Validation_Fails()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Failed("bad"));

        var handler = new ProcessApplePayWebhookCommandHandler(
            CreateService(repository.Object, validator.Object),
            NullLogger<ProcessApplePayWebhookCommandHandler>.Instance);

        var payload = "signed";
        var result = await handler.Handle(new ProcessApplePayWebhookCommand(payload, "merchant", "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bad");
    }

    [Fact]
    public async Task Handle_Should_Return_AlreadyProcessed_When_Duplicate()
    {
        var existingEvent = new BillingWebhookEvent
        {
            ExternalEventId = "tx_1",
            Provider = PaymentProviders.AppleAppStore,
            ProcessedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("tx_1", PaymentProviders.AppleAppStore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Success(
                "SUBSCRIBED",
                null,
                "tx_1",
                "orig_1",
                "prod",
                null,
                "Sandbox"));

        var handler = new ProcessApplePayWebhookCommandHandler(
            CreateService(repository.Object, validator.Object),
            NullLogger<ProcessApplePayWebhookCommandHandler>.Instance);

        var payload = "signed";
        var result = await handler.Handle(new ProcessApplePayWebhookCommand(payload, "merchant", "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.WasAlreadyProcessed.Should().BeTrue();
        result.EventId.Should().Be("tx_1");
    }

    [Fact]
    public async Task Handle_Should_Process_When_TransactionId_Present()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Failed("bad"));

        var handler = new ProcessApplePayWebhookCommandHandler(
            CreateService(repository.Object, validator.Object),
            NullLogger<ProcessApplePayWebhookCommandHandler>.Instance);

        var payload = "{\"transactionId\":\"tx\",\"eventType\":\"DID_RENEW\"}";
        var result = await handler.Handle(new ProcessApplePayWebhookCommand(payload, "merchant", "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Process_Invalid_Json_As_Unknown()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var validator = new Mock<IApplePayReceiptValidationService>();
        validator
            .Setup(v => v.VerifyNotificationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppleNotificationVerificationResult.Failed("bad"));

        var handler = new ProcessApplePayWebhookCommandHandler(
            CreateService(repository.Object, validator.Object),
            NullLogger<ProcessApplePayWebhookCommandHandler>.Instance);

        var result = await handler.Handle(new ProcessApplePayWebhookCommand("{", "merchant", "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
    }


    private static ApplePayBillingWebhookService CreateService(
        IBillingWebhookRepository repository,
        IApplePayReceiptValidationService validator)
    {
        return new ApplePayBillingWebhookService(
            repository,
            validator,
            NullLogger<ApplePayBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());
    }
}