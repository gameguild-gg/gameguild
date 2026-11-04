using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Learning.Courses;

/// <summary>
///     Options for the scheduled-publish worker (issue #1226 N10). Enabled by default —
///     realizing the schedule is the feature's entire purpose and is non-destructive —
///     and can be turned off via <c>Learning:Courses:ScheduledPublish:Enabled=false</c>;
///     default poll interval one minute.
/// </summary>
public sealed class ScheduledPublishWorkerOptions
{
    public const string SectionName = "Learning:Courses:ScheduledPublish";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 60;
}

/// <summary>
///     Scheduled-publish boundary (issue #1226 N10): realizes <c>scheduledPublishAt</c>
///     by publishing programs whose scheduled instant has arrived. Mirrors billing's
///     InvoiceAgingService / payments' PendingPaymentAgingWorker sweep semantics.
/// </summary>
public interface IPublishDueProgramsWorker
{
    /// <summary>
    ///     Processes one sweep cycle and returns the ids of the programs that transitioned
    ///     to Published. A program is a candidate when it is still in a pre-publish state
    ///     (Draft or Review), carries a <c>scheduledPublishAt</c> metadata value, and that
    ///     instant has passed. Archived or already-published programs are never touched,
    ///     so a stale schedule can neither resurrect nor re-publish them.
    /// </summary>
    Task<IReadOnlyList<Guid>> PublishDueAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PublishDueProgramsWorker(
    IApplicationDbContext context,
    ILogger<PublishDueProgramsWorker> logger) : IPublishDueProgramsWorker
{
    public async Task<IReadOnlyList<Guid>> PublishDueAsync(CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;

        // The schedule lives in the Metadata JSON column, so the server-side filter can
        // only narrow to pre-publish programs that carry metadata; the due check itself
        // is applied in memory after parsing the round-trip timestamp.
        var candidates = await context.Set<Program>()
            .Where(program => program.DeletedAt == null
                && (program.Status == ContentStatus.Draft || program.Status == ContentStatus.Review)
                && program.Metadata != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var published = new List<Guid>();
        foreach (var program in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scheduledAt = program.ScheduledPublishAt;
            if (scheduledAt is null || scheduledAt > now)
            {
                continue;
            }

            program.Status = ContentStatus.Published;
            program.Visibility = ContentVisibility.Public;
            // The schedule is consumed with the transition so subsequent sweeps (and
            // status reports) no longer consider the program scheduled.
            program.ScheduledPublishAt = null;
            program.Touch();
            published.Add(program.Id);

            logger.LogInformation(
                "Scheduled publish matured: program {ProgramId} published at {ScheduledAt:u}.",
                program.Id,
                scheduledAt);
        }

        if (published.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Scheduled-publish sweep published {Count} program(s).", published.Count);
        }

        return published;
    }
}

/// <summary>
///     Hosted service that polls for due scheduled publishes (issue #1226 N10) and runs
///     the <see cref="IPublishDueProgramsWorker"/> sweep. Anatomy mirrors the module
///     family's existing workers (billing webhook retry, payments pending-payment aging,
///     authoring AI queue): scoped worker + polling BackgroundService, config-gated.
/// </summary>
public sealed class ScheduledPublishBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledPublishWorkerOptions> workerOptions,
    ILogger<ScheduledPublishBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = workerOptions.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation(
                "ScheduledPublishBackgroundService is disabled (Learning:Courses:ScheduledPublish:Enabled=false); scheduled publishes mature on their next lifecycle touch.");
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds));
        logger.LogInformation(
            "ScheduledPublishBackgroundService started with a {Interval} poll interval.",
            pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IPublishDueProgramsWorker>()
                    .PublishDueAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled-publish sweep cycle failed");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
