using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Processes one deferred background re-scoring job (issue #409: asynchronous scoring
///     for background checks). Re-runs the fraud evaluation WITHOUT the synchronous
///     latency budget — the providers deferred when the payment path hit its budget get
///     their full evaluation here. When the completed evaluation tightens the outcome
///     (Allow → Review/Block), the payment is escalated through
///     <see cref="Payment.EscalateFraudAssessment"/> and the admins are notified through
///     the same notice the synchronous hold path uses; an unchanged or milder outcome
///     leaves the recorded decision untouched (escalation is strictly a tightening
///     operation). Per-job failures are isolated so one job never stops the worker.
/// </summary>
public interface IFraudBackgroundScoringWorker
{
    /// <summary>Processes one job. Returns true when a job was processed.</summary>
    Task<bool> ProcessOneAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class FraudBackgroundScoringWorker(
    IFraudBackgroundScoringQueue queue,
    IFraudDetectionService fraudDetectionService,
    IPaymentRepository paymentRepository,
    INotificationService? notificationService = null,
    ILogger<FraudBackgroundScoringWorker>? logger = null) : IFraudBackgroundScoringWorker
{
    /// <inheritdoc />
    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken = default)
    {
        var job = await queue.DequeueAsync(cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return false;
        }

        try
        {
            var assessment = await fraudDetectionService
                .AssessPaymentForBackgroundAsync(job.Context, cancellationToken)
                .ConfigureAwait(false);

            if (assessment.Decision == FraudDecision.Allow)
            {
                logger?.LogInformation(
                    "Background fraud re-score for payment {PaymentId} confirmed the synchronous Allow (score {Score}); {DeferredCount} deferred provider(s) completed",
                    job.PaymentId,
                    assessment.Score,
                    job.DeferredProviders.Count);
                return true;
            }

            var payment = await paymentRepository.GetByIdAsync(job.PaymentId, cancellationToken).ConfigureAwait(false);
            if (payment is null)
            {
                logger?.LogWarning(
                    "Background fraud re-score for payment {PaymentId} raised {Decision} but the payment no longer exists",
                    job.PaymentId,
                    assessment.Decision);
                return true;
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
                return true;
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Isolated per job: a failed re-score leaves the synchronous decision standing.
            logger?.LogError(
                exception,
                "Background fraud re-score for payment {PaymentId} failed; the synchronous decision stands",
                job.PaymentId);
        }

        return true;
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
///     Hosted service draining the fraud background re-scoring queue (issue #409).
///     Opt-out via <c>Payments:FraudAssessment:BackgroundRescoringEnabled</c> (safe
///     default: enabled — deferred signals complete out-of-band).
/// </summary>
public sealed class FraudBackgroundScoringHostedService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<FraudAssessmentOptions> optionsMonitor,
    ILogger<FraudBackgroundScoringHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!optionsMonitor.CurrentValue.BackgroundRescoringEnabled)
        {
            logger.LogInformation(
                "FraudBackgroundScoringHostedService is disabled (Payments:FraudAssessment:BackgroundRescoringEnabled=false); deferred signals stay deferred.");
            return;
        }

        logger.LogInformation(
            "FraudBackgroundScoringHostedService started; completing fraud providers deferred by the synchronous budget.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processed = await scope.ServiceProvider.GetRequiredService<IFraudBackgroundScoringWorker>()
                    .ProcessOneAsync(stoppingToken)
                    .ConfigureAwait(false);

                if (!processed)
                {
                    // Queue drained: back off briefly before polling again.
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken).ConfigureAwait(false);
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
        }
    }
}
