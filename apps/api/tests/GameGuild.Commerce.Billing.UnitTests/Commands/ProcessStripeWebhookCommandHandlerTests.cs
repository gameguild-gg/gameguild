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
    public async Task Handle_Should_Reject_When_Verified_EventId_Is_Missing()
    {
        var handler = new ProcessStripeWebhookCommandHandler(
            CreateService(Mock.Of<IBillingWebhookRepository>()),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        var payload = "{\"type\":\"invoice.payment_succeeded\"}";
        var act = () => handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidWebhookPayloadException>();
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
            .Setup(r => r.GetByProviderScopeAsync(
                PaymentProviders.Stripe,
                "test",
                "platform",
                "we_test",
                "evt_123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);
        existingEvent.IsProcessed = true;

        var handler = new ProcessStripeWebhookCommandHandler(
            CreateService(repository.Object),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        var payload = "{\"id\":\"evt_123\",\"type\":\"unhandled.event\"}";
        var result = await handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.WasAlreadyProcessed.Should().BeTrue();
        result.EventId.Should().Be("evt_123");
    }

    [Fact]
    public async Task Handle_Should_Return_RetryableFailure_WithoutLeakingDatabaseDetails()
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

        var payload = "{\"id\":\"evt_123\",\"type\":\"unhandled.event\"}";
        var result = await handler.Handle(new ProcessStripeWebhookCommand(payload, "sig"), CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.RequiresRetry.Should().BeTrue();
        result.ErrorMessage.Should().Be("Webhook inbox persistence failed.");
    }

    private static StripeBillingWebhookService CreateService(IBillingWebhookRepository repository)
    {
        var verifier = new Mock<IStripeWebhookVerifier>();
        verifier
            .Setup(candidate => candidate.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string payload, string _) => CreateVerifiedEvent(payload));

        return new StripeBillingWebhookService(
            repository,
            verifier.Object,
            NullLogger<StripeBillingWebhookService>.Instance,
            Mock.Of<ISubscriptionLifecycleService>(),
            Mock.Of<ISubscriptionQueryService>(),
            Mock.Of<ISubscriptionBillingService>(),
            Mock.Of<ISubscriptionExternalIdService>());
    }

    private static VerifiedStripeWebhookEvent CreateVerifiedEvent(string payload)
    {
        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idProperty) || string.IsNullOrWhiteSpace(idProperty.GetString()))
        {
            throw new InvalidWebhookPayloadException("Missing event ID in payload");
        }

        return new VerifiedStripeWebhookEvent
        {
            EventId = idProperty.GetString()!,
            EventType = root.GetProperty("type").GetString()!,
            ProviderEnvironment = "test",
            ProviderAccountId = "platform",
            WebhookEndpointId = "we_test",
            EventSchemaVersion = "2023-10-16",
            ProviderObjectId = "obj_test",
            ProviderObjectType = "test_object",
            ProviderMonetaryLeg = "nonmonetary",
            VerifiedPayload = payload,
            RetainedPayload = payload,
            PayloadSha256 = new string('0', 64)
        };
    }
}
