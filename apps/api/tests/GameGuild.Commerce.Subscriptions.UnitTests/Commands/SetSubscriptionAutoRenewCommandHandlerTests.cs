using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.SharedKernel;
using GameGuild.ValueObjects;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Commands;

public class SetSubscriptionAutoRenewCommandHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _mockRepository;
    private readonly SetSubscriptionAutoRenewCommandHandler _handler;

    public SetSubscriptionAutoRenewCommandHandlerTests()
    {
        _mockRepository = new Mock<ISubscriptionRepository>();
        _handler = new SetSubscriptionAutoRenewCommandHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldEnableAutoRenew_WhenSettingToTrue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        var command = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        subscription.AutoRenew.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDisableAutoRenew_WhenSettingToFalse()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = CreateActiveSubscription(subscriptionId);
        subscription.SetAutoRenew(true); // Start with auto-renew enabled
        var command = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: false);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription s, CancellationToken _) => s);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSubscriptionNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SubscriptionNotFoundException>()
            .WithMessage($"*{subscriptionId}*");
    }

    [Fact]
    public void Command_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var subscriptionId = Guid.NewGuid();
        var command = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: true);

        // Assert
        command.SubscriptionId.Should().Be(subscriptionId);
        command.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void Command_ShouldSupportRecordEquality()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var command1 = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: true);
        var command2 = new SetSubscriptionAutoRenewCommand(subscriptionId, AutoRenew: true);

        // Act & Assert
        command1.Should().Be(command2);
    }

    private static Subscription CreateActiveSubscription(Guid id)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, id);
        subscription.Activate();

        return subscription;
    }
}
