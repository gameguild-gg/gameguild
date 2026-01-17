using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

public class RetryWebhookEventHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Error_For_Invalid_Guid()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        var handler = new RetryWebhookEventHandler(repository.Object, NullLogger<RetryWebhookEventHandler>.Instance);

        var result = await handler.Handle(new RetryWebhookEventCommand("not-a-guid"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid event ID");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Event_Missing()
    {
        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent?)null);

        var handler = new RetryWebhookEventHandler(repository.Object, NullLogger<RetryWebhookEventHandler>.Instance);

        var result = await handler.Handle(new RetryWebhookEventCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Return_AlreadyProcessed_When_Event_Succeeded()
    {
        var webhookEvent = new BillingWebhookEvent
        {
            IsProcessed = true,
            IsFailed = false,
            ProcessingAttempts = 2
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookEvent);

        var handler = new RetryWebhookEventHandler(repository.Object, NullLogger<RetryWebhookEventHandler>.Instance);

        var result = await handler.Handle(new RetryWebhookEventCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AttemptNumber.Should().Be(2);
        result.Message.Should().Contain("already processed");
    }

    [Fact]
    public async Task Handle_Should_Reject_When_Max_Attempts_Exceeded()
    {
        var webhookEvent = new BillingWebhookEvent
        {
            IsProcessed = false,
            IsFailed = true,
            ProcessingAttempts = 5
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookEvent);

        var handler = new RetryWebhookEventHandler(repository.Object, NullLogger<RetryWebhookEventHandler>.Instance);

        var result = await handler.Handle(new RetryWebhookEventCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Maximum retry attempts");
    }

    [Fact]
    public async Task Handle_Should_Update_Event_For_Retry()
    {
        var webhookEvent = new BillingWebhookEvent
        {
            IsProcessed = false,
            IsFailed = true,
            ProcessingAttempts = 2
        };

        var repository = new Mock<IBillingWebhookRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookEvent);
        repository
            .Setup(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEvent e, CancellationToken _) => e);

        var handler = new RetryWebhookEventHandler(repository.Object, NullLogger<RetryWebhookEventHandler>.Instance);

        var result = await handler.Handle(new RetryWebhookEventCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AttemptNumber.Should().Be(3);
        webhookEvent.IsFailed.Should().BeFalse();
        repository.Verify(r => r.UpdateAsync(It.IsAny<BillingWebhookEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}