using GameGuild;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab.Services;

/// <summary>
/// Sends reminders for upcoming testing events at configurable day thresholds
/// (global TestingLabSettings default, per-event override). Runs hourly.
/// </summary>
public sealed class TestingEventReminderService(
    IServiceProvider serviceProvider,
    ILogger<TestingEventReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private const string DefaultOffsets = "4,2,1";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Testing event reminder service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                await SendDueRemindersAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Testing event reminder pass failed");
            }

            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private static async Task SendDueRemindersAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<IApplicationDbContext>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var now = SystemClock.UtcNow;

        var upcoming = await context.Set<TestingEvent>()
            .AsNoTracking()
            .Where(testingEvent => testingEvent.DeletedAt == null
                && (testingEvent.Status == TestingEventStatus.Scheduled || testingEvent.Status == TestingEventStatus.Active)
                && testingEvent.StartsAt > now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (upcoming.Count == 0)
        {
            return;
        }

        var globalOffsets = await LoadGlobalOffsetsAsync(context, cancellationToken).ConfigureAwait(false);
        var eventIds = upcoming.Select(testingEvent => testingEvent.Id).ToList();

        var recipientsByEvent = await context.Set<TestingProjectApplication>()
            .AsNoTracking()
            .Where(application => eventIds.Contains(application.EventId)
                && application.Status == TestingApplicationStatus.Approved
                && application.DeletedAt == null)
            .Select(application => new { application.EventId, application.SubmittedByUserId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var tracked = context.Set<TestingEvent>()
            .Where(testingEvent => eventIds.Contains(testingEvent.Id));

        foreach (var snapshot in upcoming)
        {
            var offsets = ParseOffsets(snapshot.ReminderDaysBeforeOverride) ?? globalOffsets;
            var daysRemaining = (int)Math.Ceiling((snapshot.StartsAt - now).TotalHours / 24);
            if (!offsets.Contains(daysRemaining))
            {
                continue;
            }

            var entity = await tracked
                .FirstAsync(testingEvent => testingEvent.Id == snapshot.Id, cancellationToken)
                .ConfigureAwait(false);
            if (entity.HasReminderBeenSent(daysRemaining))
            {
                continue;
            }

            await NotifyAsync(notificationService, snapshot.ManagerUserId, snapshot, daysRemaining, cancellationToken).ConfigureAwait(false);
            foreach (var grouping in recipientsByEvent.Where(row => row.EventId == snapshot.Id).GroupBy(row => row.SubmittedByUserId))
            {
                if (grouping.Key == snapshot.ManagerUserId)
                {
                    continue;
                }
                await NotifyAsync(notificationService, grouping.Key, snapshot, daysRemaining, cancellationToken).ConfigureAwait(false);
            }

            entity.MarkReminderSent(daysRemaining);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task NotifyAsync(INotificationService notificationService, Guid recipientId, TestingEvent testingEvent, int daysRemaining, CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.SendAsync(
                recipientId,
                NotificationType.System,
                "Testing event reminder",
                $"'{testingEvent.Name}' starts in {daysRemaining} day{(daysRemaining == 1 ? string.Empty : "s")} ({testingEvent.StartsAt:MMM d, HH:mm} UTC).",
                channel: NotificationChannel.InApp,
                tenantId: testingEvent.TenantId,
                actionUrl: $"/testing-lab/events/{testingEvent.Id}",
                referenceEntityId: testingEvent.Id,
                referenceEntityType: "TestingEvent",
                priority: daysRemaining == 1 ? NotificationPriority.High : NotificationPriority.Normal,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A single failed delivery must not block the remaining reminders.
        }
    }

    private static async Task<int[]?> LoadGlobalOffsetsAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        var raw = await context.Set<TestingLabSettings>()
            .AsNoTracking()
            .Where(settings => settings.DeletedAt == null && settings.Tenant == null)
            .Select(settings => settings.ReminderDaysBefore)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return ParseOffsets(string.IsNullOrWhiteSpace(raw) ? DefaultOffsets : raw);
    }

    private static int[]? ParseOffsets(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return null;
        }

        var offsets = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var day) ? day : 0)
            .Where(day => day is > 0 and <= 30)
            .Distinct()
            .OrderBy(day => day)
            .ToArray();
        return offsets.Length == 0 ? null : offsets;
    }
}
