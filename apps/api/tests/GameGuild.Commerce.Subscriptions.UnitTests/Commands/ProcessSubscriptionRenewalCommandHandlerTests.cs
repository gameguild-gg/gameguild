using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class ProcessSubscriptionRenewalCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUnit_WhenRenewalSucceeds()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var billingService = new Mock<ISubscriptionBillingService>();
        billingService
            .Setup(service => service.ProcessRenewalAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionRenewalResult.CreateSuccess(subscriptionId, 3, new Money(29.99m, "USD")));

        var handler = new ProcessSubscriptionRenewalCommandHandler(billingService.Object);

        // Act
        var result = await handler.Handle(new ProcessSubscriptionRenewalCommand(subscriptionId), CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenRenewalFails()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var billingService = new Mock<ISubscriptionBillingService>();
        billingService
            .Setup(service => service.ProcessRenewalAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionRenewalResult.Failed(subscriptionId, "Payment declined"));

        var handler = new ProcessSubscriptionRenewalCommandHandler(billingService.Object);

        // Act
        Func<Task> act = () => handler.Handle(new ProcessSubscriptionRenewalCommand(subscriptionId), CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<RequestValidationException>();
        exception.Which.Errors.Should().ContainSingle();
        exception.Which.Errors[0].PropertyName.Should().Be(nameof(ProcessSubscriptionRenewalCommand.SubscriptionId));
        exception.Which.Errors[0].ErrorMessage.Should().Be("Payment declined");
        exception.Which.Errors[0].AttemptedValue.Should().Be(subscriptionId);
    }
}
