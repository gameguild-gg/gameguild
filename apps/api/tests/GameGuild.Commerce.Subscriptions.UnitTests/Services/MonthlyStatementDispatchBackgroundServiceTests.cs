using System.Text.Json;
using FluentAssertions;
using GameGuild.Commerce.Subscriptions.UnitTests.Infrastructure;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public sealed class MonthlyStatementDispatchBackgroundServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static SubscriptionsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SubscriptionsTestDbContext>()
            .UseInMemoryDatabase($"statement-dispatch-{Guid.NewGuid()}")
            .Options;
        return new SubscriptionsTestDbContext(options);
    }

    private static MonthlyStatementDispatchBackgroundService CreateDispatcher(
        SubscriptionsTestDbContext context,
        Mock<INotificationService> notificationService)
    {
        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        subscriptionRepository
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var userRepository = new Mock<IUserRepository>();
        var publisher = new Mock<IPublisher>();

        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton(notificationService.Object);
        services.AddSingleton(subscriptionRepository.Object);
        services.AddSingleton(userRepository.Object);
        services.AddSingleton(publisher.Object);
        var provider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().Build();
        var linkBuilder = new Mock<IMonthlyStatementLinkBuilder>();
        linkBuilder
            .Setup(l => l.Build(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new MonthlyStatementLinks("My Workspace", "/billing", "/statements", "/statements.pdf", "/statements.csv"));

        return new MonthlyStatementDispatchBackgroundService(
            provider,
            configuration,
            linkBuilder.Object,
            NullLogger<MonthlyStatementDispatchBackgroundService>.Instance);
    }

    private static Notification CreateStatementRow(DateOnly fromDate)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            tenantId = TenantId,
            subscriptionId = SubscriptionId,
            userId = UserId,
            fromDate = $"{fromDate:yyyy-MM-dd}",
            toDate = "2026-07-31",
            workspaceLabel = "My Workspace",
            monthLabel = "July 2026",
            recipientEmail = "member@example.com",
            recipientName = "Member Name",
        });

        return Notification.Create(
            UserId,
            NotificationType.MonthlyStatement,
            NotificationChannel.Email,
            "Your statement for July 2026 is ready",
            "message",
            metadata: metadata);
    }

    [Fact]
    public async Task SendStatementsForPeriodAsync_WhenPeriodAlreadyQueued_CreatesNoNewRows()
    {
        var context = CreateContext();
        context.Notifications.Add(CreateStatementRow(new DateOnly(2026, 7, 1)));
        await context.SaveChangesAsync();

        var notificationService = new Mock<INotificationService>();
        var dispatcher = CreateDispatcher(context, notificationService);

        // Period for July 2026 statement = August 1st (period.AddMonths(-1) = July).
        await dispatcher.SendStatementsForPeriodAsync(new DateOnly(2026, 8, 1), CancellationToken.None);

        notificationService.Verify(
            s => s.SendAsync(
                It.IsAny<Guid>(),
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
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendStatementsForPeriodAsync_WhenPeriodNotQueued_CreatesEmailRowPerSubscription()
    {
        var context = CreateContext();

        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(s => s.SendAsync(
                It.IsAny<Guid>(),
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
            .ReturnsAsync(Result.Success(Notification.Create(
                UserId,
                NotificationType.MonthlyStatement,
                NotificationChannel.Email,
                "title",
                "message")));

        var subscriptionRepository = new Mock<ISubscriptionRepository>();
        var subscription = new Subscription(TenantId, Guid.NewGuid(), UserId, BillingCycle.Monthly, new Money(2999), DateTime.UtcNow);
        subscriptionRepository
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subscription]);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = UserId, Email = "member@example.com", Name = "Member Name", IsActive = true });

        var publisher = new Mock<IPublisher>();

        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton(notificationService.Object);
        services.AddSingleton(subscriptionRepository.Object);
        services.AddSingleton(userRepository.Object);
        services.AddSingleton(publisher.Object);
        var provider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().Build();
        var linkBuilder = new Mock<IMonthlyStatementLinkBuilder>();
        linkBuilder
            .Setup(l => l.Build(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new MonthlyStatementLinks("My Workspace", "/billing", "/statements", "/statements.pdf", "/statements.csv"));

        var dispatcher = new MonthlyStatementDispatchBackgroundService(
            provider,
            configuration,
            linkBuilder.Object,
            NullLogger<MonthlyStatementDispatchBackgroundService>.Instance);

        await dispatcher.SendStatementsForPeriodAsync(new DateOnly(2026, 8, 1), CancellationToken.None);

        notificationService.Verify(
            s => s.SendAsync(
                UserId,
                NotificationType.MonthlyStatement,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationChannel.Email,
                TenantId,
                It.IsAny<string?>(),
                NotificationPriority.Normal,
                subscription.Id,
                nameof(Subscription),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
