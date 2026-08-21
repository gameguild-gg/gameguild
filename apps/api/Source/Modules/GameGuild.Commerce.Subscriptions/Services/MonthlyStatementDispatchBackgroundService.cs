using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GameGuild.Commerce.Subscriptions;

public sealed class MonthlyStatementDispatchBackgroundService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IMonthlyStatementLinkBuilder statementLinkBuilder,
    ILogger<MonthlyStatementDispatchBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);
    private bool _databaseUnavailableLogged;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MonthlyStatementDispatchBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await IsDatabaseReachableAsync(stoppingToken).ConfigureAwait(false))
                {
                    if (!_databaseUnavailableLogged)
                    {
                        logger.LogInformation("Monthly statements are paused because the database is unreachable.");
                        _databaseUnavailableLogged = true;
                    }
                }
                else
                {
                    _databaseUnavailableLogged = false;
                    var nowUtc = DateTime.UtcNow;
                    if (nowUtc.Day == 1)
                    {
                        var period = new DateOnly(nowUtc.Year, nowUtc.Month, 1);
                        await SendStatementsForPeriodAsync(period, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monthly statement run failed; will retry on next tick");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    internal async Task SendStatementsForPeriodAsync(DateOnly period, CancellationToken ct)
    {
        var statementMonth = period.AddMonths(-1);
        var monthLabel = statementMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var fromDate = new DateOnly(statementMonth.Year, statementMonth.Month, 1);
        var toDate = new DateOnly(
            statementMonth.Year,
            statementMonth.Month,
            DateTime.DaysInMonth(statementMonth.Year, statementMonth.Month));

        using var scope = serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Idempotency: a restart on the 1st must never re-publish a full period. The period is identified by
        // its fromDate, which every MonthlyStatement row for the period carries in its metadata JSON. If any
        // such row already exists, the period was already queued — skip it entirely. (ponytail: per-period
        // guard; a crash mid-run that leaves some subscriptions unqueued is accepted over re-publishing the
        // whole period to everyone. Per-subscription tracking would need a metadata parse per row.)
        var periodAlreadyQueued = await dbContext.Set<Notification>()
            .AnyAsync(
                n => n.Type == NotificationType.MonthlyStatement
                    && n.Metadata != null
                    && n.Metadata.Contains($"\"fromDate\":\"{fromDate:yyyy-MM-dd}\""),
                ct)
            .ConfigureAwait(false);

        if (periodAlreadyQueued)
        {
            logger.LogInformation(
                "Monthly statement period {MonthLabel} already queued; skipping to avoid duplicates.",
                monthLabel);
            return;
        }

        var activeSubscriptions = await subscriptionRepository
            .GetByStatusAsync(SubscriptionStatus.Active, ct)
            .ConfigureAwait(false);

        var workspaceLabel = statementLinkBuilder.Build(fromDate, toDate).WorkspaceLabel;

        var queued = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var subscription in activeSubscriptions)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var tenantId = ((ISubscription)subscription).TenantId;
                if (tenantId == Guid.Empty)
                {
                    failed++;
                    logger.LogWarning(
                        "Skipping monthly statement dispatch for subscription {SubscriptionId} because TenantId is missing.",
                        subscription.Id);
                    continue;
                }

                var recipient = await userRepository
                    .GetByIdAsync(subscription.CreatedByUserId, ct)
                    .ConfigureAwait(false);

                if (recipient is null || !recipient.IsActive || recipient.IsSuspended || string.IsNullOrWhiteSpace(recipient.Email))
                {
                    failed++;
                    logger.LogWarning(
                        "Skipping monthly statement dispatch for subscription {SubscriptionId} because the recipient user record is unavailable or cannot receive mail.",
                        subscription.Id);
                    continue;
                }

                var metadata = JsonSerializer.Serialize(new
                {
                    tenantId,
                    subscriptionId = subscription.Id,
                    userId = recipient.Id,
                    fromDate = $"{fromDate:yyyy-MM-dd}",
                    toDate = $"{toDate:yyyy-MM-dd}",
                    workspaceLabel,
                    monthLabel,
                    recipientEmail = recipient.Email,
                    recipientName = recipient.Name,
                });

                // Keep the publish+handler as the InApp row-creation step (existing dashboard behavior).
                await publisher.Publish(
                    new MonthlyStatementPreparedNotification
                    {
                        SubscriptionId = subscription.Id,
                        TenantId = tenantId,
                        RecipientId = recipient.Id,
                        RecipientEmail = recipient.Email,
                        RecipientName = recipient.Name,
                        WorkspaceLabel = workspaceLabel,
                        MonthLabel = monthLabel,
                        FromDate = fromDate,
                        ToDate = toDate,
                    },
                    ct).ConfigureAwait(false);

                var result = await notificationService.SendAsync(
                    recipientId: recipient.Id,
                    type: NotificationType.MonthlyStatement,
                    title: $"Your statement for {monthLabel} is ready",
                    message:
                        $"Your monthly statement for {monthLabel} is now available. " +
                        $"The PDF and CSV copies are attached to this email, and the {workspaceLabel} has the same statement available online.",
                    channel: NotificationChannel.Email,
                    tenantId: tenantId,
                    priority: GameGuild.Notifications.NotificationPriority.Normal,
                    referenceEntityId: subscription.Id,
                    referenceEntityType: nameof(Subscription),
                    metadata: metadata,
                    cancellationToken: ct).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    queued++;
                }
                else
                {
                    // Preference enforcement (mute/digest/quiet-hours) intentionally produced no row.
                    skipped++;
                    logger.LogInformation(
                        "Monthly statement for subscription {SubscriptionId} not queued: {Reason}",
                        subscription.Id,
                        result.Error?.Description ?? "preference decision");
                }
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(
                    ex,
                    "Failed to queue monthly statement dispatch for subscription {SubscriptionId}",
                    subscription.Id);
            }
        }

        logger.LogInformation(
            "Monthly statement run for {Period}: {Queued} queued, {Skipped} skipped, {Failed} failed",
            monthLabel,
            queued,
            skipped,
            failed);
    }

    private async Task<bool> IsDatabaseReachableAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        NpgsqlConnectionStringBuilder connectionStringBuilder;
        try
        {
            connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Host) || connectionStringBuilder.Port <= 0)
        {
            return false;
        }

        var hosts = connectionStringBuilder.Host
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (hosts.Length == 0)
        {
            return false;
        }

        foreach (var host in hosts)
        {
            if (!await CanConnectAsync(host, connectionStringBuilder.Port, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> CanConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProbeTimeout);

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, timeoutCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
