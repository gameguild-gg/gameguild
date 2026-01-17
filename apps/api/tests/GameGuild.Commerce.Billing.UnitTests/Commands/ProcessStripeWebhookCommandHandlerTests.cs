using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

public class ProcessStripeWebhookCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Failed_When_EventId_Missing()
    {
        var handler = new ProcessStripeWebhookCommandHandler(
            CreateService(Mock.Of<IBillingWebhookRepository>()),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        var payload = "{\"type\":\"invoice.payment_succeeded\"}";
        var result = await handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Missing event ID");
    }

    [Fact]
    public async Task Handle_Should_Return_Result_From_Service()
    {
        var existingEvent = new BillingWebhookEvent
        {
            ExternalEventId = "evt_123",
            Provider = PaymentProviders.Stripe,
            ProcessedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("evt_123", PaymentProviders.Stripe, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        var handler = new ProcessStripeWebhookCommandHandler(
            CreateService(repository.Object),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        var payload = "{\"id\":\"evt_123\",\"type\":\"invoice.payment_succeeded\"}";
        var result = await handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.WasAlreadyProcessed.Should().BeTrue();
        result.EventId.Should().Be("evt_123");
    }

    [Fact]
    public async Task Handle_Should_Return_Failed_When_Service_Throws()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByExternalEventIdAsync("evt_123", PaymentProviders.Stripe, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new ProcessStripeWebhookCommandHandler(
            CreateService(repository.Object),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        var payload = "{\"id\":\"evt_123\",\"type\":\"invoice.payment_succeeded\"}";
        var result = await handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("boom");
    }

    private static StripeBillingWebhookService CreateService(IBillingWebhookRepository repository)
    {
        return new StripeBillingWebhookService(
            repository,
            NullLogger<StripeBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());
    }
}