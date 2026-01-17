using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Queries;

public class GetWebhookEventHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Null_For_Invalid_Id()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var handler = new GetWebhookEventHandler(repository.Object, NullLogger<GetWebhookEventHandler>.Instance);

        var result = await handler.Handle(new GetWebhookEventQuery("not-a-guid"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);

        var handler = new GetWebhookEventHandler(repository.Object, NullLogger<GetWebhookEventHandler>.Instance);

        var result = await handler.Handle(new GetWebhookEventQuery(Guid.NewGuid().ToString()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Map_Event_To_Dto()
    {
        var webhookEvent = new BillingWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = PaymentProviders.Stripe,
            ExternalEventId = "evt_1",
            EventType = "invoice.payment_succeeded",
            IsProcessed = true,
            ProcessingAttempts = 1,
            CreatedAt = DateTime.UtcNow
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookEvent);

        var handler = new GetWebhookEventHandler(repository.Object, NullLogger<GetWebhookEventHandler>.Instance);

        var result = await handler.Handle(new GetWebhookEventQuery(webhookEvent.Id.ToString()), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ExternalEventId.Should().Be("evt_1");
        result.Provider.Should().Be(PaymentProviders.Stripe);
    }
}