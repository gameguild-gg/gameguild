using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using GameGuild.API.Integration;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;

namespace GameGuild.API.UnitTests.Integration;

/// <summary>
///     Payment-method confirmation emails (issue #414): adding or removing a saved method
///     queues an Email-channel Billing notification to the owner with display-safe card
///     metadata; delivery problems never throw.
/// </summary>
public sealed class PaymentMethodEmailHandlerTests
{
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IMonthlyStatementLinkBuilder> _linkBuilder = new();

    public PaymentMethodEmailHandlerTests()
    {
        _linkBuilder.Setup(builder => builder.GetBillingDashboardPath()).Returns("/billing");
    }

    private static Result<Notification> Delivered() => Result.Success((Notification?)null!);

    [Fact]
    public async Task Registered_SendsBillingConfirmationToOwner()
    {
        var paymentMethodId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(null!));

        var handler = new PaymentMethodRegisteredEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodRegisteredEmailHandler>>());

        await handler.Handle(
            new PaymentMethodRegisteredEvent(paymentMethodId, tenantId, userId, "pm_new_card", "visa", "4242"),
            CancellationToken.None);

        _notificationService.Verify(
            service => service.SendAsync(
                userId,
                NotificationType.Billing,
                "Payment method added",
                It.Is<string>(message => message.Contains("visa card ending in 4242") && message.Contains("added")),
                NotificationChannel.Email,
                tenantId,
                "/billing",
                NotificationPriority.Normal,
                paymentMethodId,
                nameof(PaymentMethod),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Registered_WithoutCardMetadata_UsesGenericLabel()
    {
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(null!));

        var handler = new PaymentMethodRegisteredEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodRegisteredEmailHandler>>());

        await handler.Handle(
            new PaymentMethodRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "pm_bank", cardBrand: null, cardLast4: null),
            CancellationToken.None);

        _notificationService.Verify(
            service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.Is<string>(message => message.Contains("payment method was added")),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Registered_RestoredEvent_SaysReAdded()
    {
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(null!));

        var handler = new PaymentMethodRegisteredEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodRegisteredEmailHandler>>());

        await handler.Handle(
            new PaymentMethodRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "pm_back", "visa", "4242", restored: true),
            CancellationToken.None);

        _notificationService.Verify(
            service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                "Payment method re-added",
                It.Is<string>(message => message.Contains("re-added")),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Deleted_SendsBillingConfirmationToOwner()
    {
        var paymentMethodId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Notification>(null!));

        var handler = new PaymentMethodDeletedEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodDeletedEmailHandler>>());

        await handler.Handle(
            new PaymentMethodDeletedEvent(paymentMethodId, tenantId, userId, "mastercard", "1881"),
            CancellationToken.None);

        _notificationService.Verify(
            service => service.SendAsync(
                userId,
                NotificationType.Billing,
                "Payment method removed",
                It.Is<string>(message => message.Contains("mastercard card ending in 1881") && message.Contains("removed")),
                NotificationChannel.Email,
                tenantId,
                "/billing",
                NotificationPriority.Normal,
                paymentMethodId,
                nameof(PaymentMethod),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handlers_WhenSendThrows_DoNotPropagate()
    {
        _notificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("queue down"));

        var registeredHandler = new PaymentMethodRegisteredEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodRegisteredEmailHandler>>());
        var deletedHandler = new PaymentMethodDeletedEmailHandler(
            _notificationService.Object,
            _linkBuilder.Object,
            Mock.Of<ILogger<PaymentMethodDeletedEmailHandler>>());

        var registeredAct = async () => await registeredHandler.Handle(
            new PaymentMethodRegisteredEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "pm_x"),
            CancellationToken.None);
        var deletedAct = async () => await deletedHandler.Handle(
            new PaymentMethodDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await registeredAct.Should().NotThrowAsync();
        await deletedAct.Should().NotThrowAsync();
    }
}
