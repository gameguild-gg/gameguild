using System.Reflection;
using FluentAssertions;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using GameGuild.TestingLab.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventReminderServiceCoverageTests
{
    [Fact]
    public async Task ReminderPass_NotifiesManagersAndUniqueApplicantsAndPersistsDeliveryMarkers()
    {
        await using var context = Context();
        context.TestingLabSettings.Add(new TestingLabSettings
        {
            TenantId = null,
            Tenant = null,
            ReminderDaysBefore = "2, invalid, 0, 31, 2"
        });
        var now = SystemClock.UtcNow;
        var managerOne = Guid.NewGuid();
        var managerTwo = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var oneDay = ScheduledEvent(now.AddDays(1), managerOne);
        oneDay.SetReminderOverride([1]);
        var twoDays = ScheduledEvent(now.AddDays(2), managerTwo);
        var notDue = ScheduledEvent(now.AddDays(5), Guid.NewGuid());
        notDue.SetReminderOverride([1]);
        var alreadySent = ScheduledEvent(now.AddDays(1), Guid.NewGuid());
        alreadySent.SetReminderOverride([1]);
        alreadySent.MarkReminderSent(1);
        context.TestingEvents.AddRange(oneDay, twoDays, notDue, alreadySent);
        context.TestingProjectApplications.AddRange(
            ApprovedApplication(oneDay.Id, recipient),
            ApprovedApplication(oneDay.Id, recipient),
            ApprovedApplication(oneDay.Id, managerOne));
        await context.SaveChangesAsync();

        var notifications = NotificationService();
        using var provider = Services(context, notifications.Object);

        await InvokeReminderPass(provider);

        notifications.Verify(service => service.SendAsync(
            It.IsAny<Guid?>(), NotificationType.System, "Testing event reminder",
            It.IsAny<string>(), NotificationChannel.InApp, It.IsAny<Guid?>(),
            It.Is<string?>(url => url != null && url.Contains("/testing-lab/events/")),
            It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), "TestingEvent",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        notifications.Verify(service => service.SendAsync(
            recipient, It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(service => service.SendAsync(
            managerOne, It.IsAny<NotificationType>(), It.IsAny<string>(), It.Is<string>(message => message.Contains("1 day ")),
            It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(), NotificationPriority.High,
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(service => service.SendAsync(
            managerTwo, It.IsAny<NotificationType>(), It.IsAny<string>(), It.Is<string>(message => message.Contains("2 days ")),
            It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(), NotificationPriority.Normal,
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        oneDay.HasReminderBeenSent(1).Should().BeTrue();
        twoDays.HasReminderBeenSent(2).Should().BeTrue();
        notDue.HasReminderBeenSent(1).Should().BeFalse();
    }

    [Fact]
    public async Task ReminderPass_ReturnsWithoutNotificationsWhenNoEventsAreUpcoming()
    {
        await using var context = Context();
        var notifications = NotificationService();
        using var provider = Services(context, notifications.Object);

        await InvokeReminderPass(provider);

        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FailedNotification_DoesNotPreventMarkersOrRemainingDeliveries()
    {
        await using var context = Context();
        var manager = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var testingEvent = ScheduledEvent(SystemClock.UtcNow.AddDays(1), manager);
        testingEvent.SetReminderOverride([1]);
        context.TestingEvents.Add(testingEvent);
        context.TestingProjectApplications.Add(ApprovedApplication(testingEvent.Id, recipient));
        await context.SaveChangesAsync();
        var notifications = NotificationService();
        notifications.Setup(service => service.SendAsync(
                manager, It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delivery failed"));
        using var provider = Services(context, notifications.Object);

        await InvokeReminderPass(provider);

        testingEvent.HasReminderBeenSent(1).Should().BeTrue();
        notifications.Verify(service => service.SendAsync(
            recipient, It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OffsetParser_NormalizesNullInvalidDuplicateAndOrderedValues()
    {
        ParseOffsets(null).Should().BeNull();
        ParseOffsets(" ").Should().BeNull();
        ParseOffsets("invalid,0,31").Should().BeNull();
        ParseOffsets("7, 1,7,2").Should().Equal(1, 2, 7);
    }

    [Fact]
    public async Task HostedService_StopsCleanlyWhenAlreadyCancelled()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var service = new TestingEventReminderService(
            provider, NullLogger<TestingEventReminderService>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await service.StartAsync(cancellation.Token);
        await service.StopAsync(CancellationToken.None);
    }

    private static ReminderContext Context()
    {
        var options = new DbContextOptionsBuilder<ReminderContext>()
            .UseInMemoryDatabase($"testing-reminders-{Guid.NewGuid():N}")
            .Options;
        return new ReminderContext(options);
    }

    private static ServiceProvider Services(ReminderContext context, INotificationService notifications)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton(notifications);
        return services.BuildServiceProvider();
    }

    private static Mock<INotificationService> NotificationService()
    {
        var notifications = new Mock<INotificationService>();
        notifications.Setup(service => service.SendAsync(
                It.IsAny<Guid?>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid? recipientId, NotificationType type, string title, string message,
                NotificationChannel channel, Guid? tenantId, string? actionUrl,
                NotificationPriority priority, Guid? referenceEntityId, string? referenceEntityType,
                string? metadata, string? recipientEmail, CancellationToken _) =>
                Result.Success(Notification.Create(recipientId, type, channel, title, message, tenantId,
                    actionUrl: actionUrl,
                    priority: priority,
                    referenceEntityId: referenceEntityId,
                    referenceEntityType: referenceEntityType,
                    metadata: metadata,
                    recipientEmail: recipientEmail)));
        return notifications;
    }

    private static TestingEvent ScheduledEvent(DateTime startsAt, Guid managerId)
    {
        var testingEvent = TestingEvent.Create(
            $"Event {Guid.NewGuid():N}", TestingEventMode.Online, managerId,
            startsAt.AddDays(-4), startsAt.AddDays(-2), startsAt, startsAt.AddHours(2),
            true, TestingEventApprovalMode.ManagerOnly, Guid.NewGuid());
        testingEvent.OpenConfiguredApplications();
        testingEvent.CloseApplications();
        testingEvent.Schedule();
        return testingEvent;
    }

    private static TestingProjectApplication ApprovedApplication(Guid eventId, Guid submitterId)
    {
        var application = TestingProjectApplication.Submit(
            eventId, Guid.NewGuid(), null, submitterId, null, Guid.NewGuid());
        application.Approve(Guid.NewGuid(), Guid.NewGuid(), null);
        return application;
    }

    private static async Task InvokeReminderPass(IServiceProvider provider)
    {
        var method = typeof(TestingEventReminderService).GetMethod(
            "SendDueRemindersAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        await (Task)method.Invoke(null, [provider, CancellationToken.None])!;
    }

    private static int[]? ParseOffsets(string? csv)
    {
        var method = typeof(TestingEventReminderService).GetMethod(
            "ParseOffsets", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (int[]?)method.Invoke(null, [csv]);
    }

    private sealed class ReminderContext(DbContextOptions<ReminderContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();
        public DbSet<TestingLabSettings> TestingLabSettings => Set<TestingLabSettings>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
