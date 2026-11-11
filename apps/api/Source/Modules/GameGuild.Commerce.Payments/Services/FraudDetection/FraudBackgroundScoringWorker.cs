using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Processes one polling pass of durable deferred background re-scoring jobs (issue #409:
///     asynchronous scoring for background checks; issue #1218 G3: durable handoff). Re-runs the
///     fraud evaluation WITHOUT the synchronous latency budget — the providers deferred when the
///     payment path hit its budget get their full evaluation here. When the completed evaluation
///     tightens the outcome (Allow → Review/Block), the payment is escalated through
///     <see cref="Payment.EscalateFraudAssessment"/> and the admins are notified through the
///     same notice the synchronous hold path uses; an unchanged or milder outcome leaves the
///     recorded decision untouched (escalation is strictly a tightening operation). Per-job
///     failures are isolated so one job never stops the pass; exhausted jobs dead-letter and the
///     synchronous decision stands.
/// </summary>
public interface IFraudBackgroundScoringWorker
{
    /// <summary>Processes one pass of due jobs. Returns the number of jobs processed.</summary>
    Task<int> ProcessDueAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class FraudBackgroundScoringWorker(
    IFraudBackgroundScoringJobStore jobStore,
    IFraudDetectionService fraudDetectionService,
    IPaymentRepository paymentRepository,
    INotificationService? notificationService = null,
    ILogger<FraudBackgroundScoringWorker>? logger = null) : IFraudBackgroundScoringWorker
{
    private const int BatchSize = 25;

    /// <inheritdoc />
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}";
        var jobs = await jobStore
            .ClaimDueAsync(BatchSize, workerId, cancellationToken)
            .ConfigureAwait(false);

        var processed = 0;
        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessOneAsync(job, cancellationToken).ConfigureAwait(false);
            processed++;
        }

        return processed;
    }

    private async Task ProcessOneAsync(FraudBackgroundScoringJob job, CancellationToken cancellationToken)
    {
        if (!job.TryRehydrateContext(out var context) || context is null)
        {
            // Poison payload: the row can never be processed — dead-letter immediately
            // instead of burning the attempt budget on it.
            logger?.LogError(
                "Background fraud re-scoring job {JobId} for payment {PaymentId} carries a corrupt signal context; dead-lettered",
                job.Id,
                job.PaymentId);
            await jobStore.DeadLetterAsync(job, "Corrupt signal context payload.", cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            var assessment = await fraudDetectionService
                .AssessPaymentForBackgroundAsync(context, cancellationToken)
                .ConfigureAwait(false);

            if (assessment.Decision == FraudDecision.Allow)
            {
                logger?.LogInformation(
                    "Background fraud re-score for payment {PaymentId} confirmed the synchronous Allow (score {Score})",
                    job.PaymentId,
                    assessment.Score);
                await jobStore.CompleteAsync(job, cancellationToken).ConfigureAwait(false);
                return;
            }

            var payment = await paymentRepository.GetByIdAsync(job.PaymentId, cancellationToken).ConfigureAwait(false);
            if (payment is null)
            {
                logger?.LogWarning(
                    "Background fraud re-score for payment {PaymentId} raised {Decision} but the payment no longer exists",
                    job.PaymentId,
                    assessment.Decision);
                await jobStore.CompleteAsync(job, cancellationToken).ConfigureAwait(false);
                return;
            }

            var before = payment.FraudDecision;
            var escalated = payment.EscalateFraudAssessment(
                assessment.Decision,
                assessment.Score,
                assessment.Reason,
                assessment.ToEvidenceJson());
            if (!escalated)
            {
                // The recorded decision already stood at Review/Block: no write, no notice.
                logger?.LogInformation(
                    "Background fraud re-score for payment {PaymentId} confirmed the recorded {Decision}; nothing to escalate",
                    job.PaymentId,
                    before);
                await jobStore.CompleteAsync(job, cancellationToken).ConfigureAwait(false);
                return;
            }

            await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

            logger?.LogWarning(
                "Background fraud re-score escalated payment {PaymentId} from {Before} to {Decision} (score {Score}): {Reason}",
                job.PaymentId,
                before,
                assessment.Decision,
                assessment.Score,
                assessment.Reason);

            await NotifyEscalationAsync(payment, assessment, cancellationToken).ConfigureAwait(false);
            await jobStore.CompleteAsync(job, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Isolated per job: a failed re-score re-pends with backoff, dead-letters after the
            // attempt ceiling, and the synchronous decision stands throughout.
            logger?.LogError(
                exception,
                "Background fraud re-score for payment {PaymentId} failed (attempt {Attempt}); the synchronous decision stands",
                job.PaymentId,
                job.AttemptCount);
            await jobStore
                .FailAsync(job, exception.Message, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Best-effort admin notice for a background escalation, mirroring the synchronous
    ///     fraud-hold admin notice. Delivery problems never change the escalation.
    /// </summary>
    private async Task NotifyEscalationAsync(
        Payment payment,
        FraudAssessment assessment,
        CancellationToken cancellationToken)
    {
        if (notificationService is null)
        {
            return;
        }

        try
        {
            var result = await notificationService
                .SendAsync(
                    recipientId: Guid.Empty, // System-level notice — routed to the admin channel
                    type: NotificationType.System,
                    title: "Payment escalated by the background fraud re-score",
                    message:
                        $"Payment {payment.Id} ({payment.Amount:0.##} {payment.Currency}) was escalated to {assessment.Decision} " +
                        $"by the background re-score (score {assessment.Score}): {assessment.Reason}. " +
                        (assessment.Decision == FraudDecision.Review
                            ? "Review it in the fraud review queue."
                            : "The payment was held without an automatic retry."),
                    channel: NotificationChannel.InApp,
                    tenantId: payment.TenantId,
                    actionUrl: null,
                    priority: assessment.Decision == FraudDecision.Block ? NotificationPriority.Urgent : NotificationPriority.High,
                    referenceEntityId: payment.Id,
                    referenceEntityType: nameof(Payment),
                    recipientEmail: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger?.LogInformation(
                    "Fraud escalation notice for payment {PaymentId} was not delivered: {Reason}",
                    payment.Id,
                    result.Error?.Description ?? "preference decision");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger?.LogWarning(
                exception,
                "Failed to send the fraud escalation notice for payment {PaymentId}",
                payment.Id);
        }
    }
}

/// <summary>
///     Hosted service draining the durable fraud background re-scoring queue (issues #409 and
///     #1218 G3). The due sweep replaces the volatile in-memory channel: deferred jobs survive
///     restarts and are never silently dropped. Opt-out via
///     <c>Payments:FraudAssessment:BackgroundRescoringEnabled</c> (safe default: enabled —
///     deferred signals complete out-of-band).
/// </summary>
public sealed class FraudBackgroundScoringHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<FraudAssessmentOptions> optionsMonitor,
    ILogger<FraudBackgroundScoringHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!optionsMonitor.CurrentValue.BackgroundRescoringEnabled)
        {
            logger.LogInformation(
                "FraudBackgroundScoringHostedService is disabled (Payments:FraudAssessment:BackgroundRescoringEnabled=false); deferred signals stay deferred.");
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(Math.Max(
            1,
            optionsMonitor.CurrentValue.BackgroundRescoringPollIntervalSeconds));
        logger.LogInformation(
            "FraudBackgroundScoringHostedService started; completing fraud providers deferred by the synchronous budget from the durable queue.");

        await Task.Delay(InitialDelay, stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processed = await scope.ServiceProvider.GetRequiredService<IFraudBackgroundScoringWorker>()
                    .ProcessDueAsync(stoppingToken)
                    .ConfigureAwait(false);

                if (processed > 0)
                {
                    logger.LogInformation(
                        "Fraud background re-scoring pass processed {Count} deferred job(s)",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Fraud background re-scoring cycle failed");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
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
