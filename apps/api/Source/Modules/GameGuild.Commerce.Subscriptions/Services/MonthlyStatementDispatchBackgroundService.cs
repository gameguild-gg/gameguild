using System.Globalization;
using System.Net.Sockets;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
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
    private DateOnly _lastProcessedPeriod = DateOnly.MinValue;
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
                        if (period != _lastProcessedPeriod)
                        {
                            await SendStatementsForPeriodAsync(period, stoppingToken).ConfigureAwait(false);
                            _lastProcessedPeriod = period;
                        }
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

    private async Task SendStatementsForPeriodAsync(DateOnly period, CancellationToken ct)
    {
        var statementMonth = period.AddMonths(-1);
        var monthLabel = statementMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var fromDate = new DateOnly(statementMonth.Year, statementMonth.Month, 1);
        var toDate = new DateOnly(
            statementMonth.Year,
            statementMonth.Month,
            DateTime.DaysInMonth(statementMonth.Year, statementMonth.Month));
        var links = statementLinkBuilder.Build(fromDate, toDate);
        var statementPagePath = links.StatementPagePath;
        var statementPdfPath = links.StatementPdfPath;
        var statementCsvPath = links.StatementCsvPath;
        var consoleBaseUrl = ResolveConsoleBaseUrl();
        var statementPageAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, statementPagePath);
        var statementPdfAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, statementPdfPath);
        var statementCsvAbsoluteUrl = BuildAbsoluteUrl(consoleBaseUrl, statementCsvPath);

        using var scope = serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var attachmentBuilder = scope.ServiceProvider.GetRequiredService<IMonthlyStatementAttachmentBuilder>();

        var activeSubscriptions = await subscriptionRepository
            .GetByStatusAsync(SubscriptionStatus.Active, ct)
            .ConfigureAwait(false);

        var queued = 0;
        var failed = 0;
        var cachedArtifactsByTenant = new Dictionary<Guid, MonthlyStatementArtifacts>();

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

                if (!cachedArtifactsByTenant.TryGetValue(tenantId, out var artifacts))
                {
                    artifacts = await attachmentBuilder
                        .BuildAsync(tenantId, fromDate, toDate, ct)
                        .ConfigureAwait(false);
                    cachedArtifactsByTenant[tenantId] = artifacts;
                }

                await publisher.Publish(
                    new MonthlyStatementPreparedNotification
                    {
                        SubscriptionId = subscription.Id,
                        TenantId = tenantId,
                        RecipientId = recipient.Id,
                        RecipientEmail = recipient.Email,
                        RecipientName = recipient.Name,
                        WorkspaceLabel = links.WorkspaceLabel,
                        MonthLabel = monthLabel,
                        FromDate = fromDate,
                        ToDate = toDate,
                        StatementPagePath = statementPagePath,
                        StatementPdfPath = statementPdfPath,
                        StatementCsvPath = statementCsvPath,
                        StatementPageAbsoluteUrl = statementPageAbsoluteUrl,
                        StatementPdfAbsoluteUrl = statementPdfAbsoluteUrl,
                        StatementCsvAbsoluteUrl = statementCsvAbsoluteUrl,
                        Artifacts = artifacts,
                    },
                    ct).ConfigureAwait(false);

                queued++;
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
            "Monthly statement run for {Period}: {Queued} queued, {Failed} failed",
            monthLabel,
            queued,
            failed);
    }

    private string ResolveConsoleBaseUrl()
    {
        var configured = configuration["StatementEmails:ConsoleBaseUrl"]
            ?? configuration["NEXTAUTH_URL"]
            ?? configuration["NEXT_PUBLIC_URL"]
            ?? "http://localhost:3000";

        return configured.Trim().TrimEnd('/');
    }

    private static string BuildAbsoluteUrl(string baseUrl, string relativePath)
        => new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/", UriKind.Absolute), relativePath.TrimStart('/')).ToString();

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
