using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Aging boundary for outcome-unknown payments stuck in <see cref="PaymentStatus.Pending" />
///     (issue #1202 gaps N6), mirroring billing's InvoiceAgingService: a bounded sweep moves
///     aged rows to a terminal, human-visible state with a recorded reason instead of leaving
///     them in limbo forever.
/// </summary>
public interface IPendingPaymentAgingWorker
{
    /// <summary>
    ///     Processes one sweep cycle and returns the number of payments exited as failed
    ///     with reason. A payment is a candidate when it has been Pending longer than the
    ///     configured timeout, never observed a gateway outcome (<c>ProcessedAt</c> is null)
    ///     and has no complete provider mapping — the exact population the webhook cannot
    ///     resolve and the retry queue never selects.
    /// </summary>
    Task<int> ExitStuckPendingPaymentsAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PendingPaymentAgingWorker(
    IPaymentRepository paymentRepository,
    IOptions<PaymentAgingOptions> agingOptions,
    ILogger<PendingPaymentAgingWorker> logger) : IPendingPaymentAgingWorker
{
    /// <summary>
    ///     Stable failure code recorded on swept payments so operators can distinguish the
    ///     aging exit from gateway declines and reconcile the charge with the provider before
    ///     any manual retry.
    /// </summary>
    public const string FailureCode = "payment_outcome_unknown_aged";

    /// <inheritdoc />
    public async Task<int> ExitStuckPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var settings = agingOptions.Value;
        var stuckBefore = SystemClock.UtcNow.AddHours(-settings.StuckPendingTimeoutHours);

        var stuck = (await paymentRepository
            .GetStuckPendingAsync(stuckBefore, cancellationToken, take: settings.MaxBatchSize)
            .ConfigureAwait(false))
            .ToList();

        var exited = 0;
        foreach (var payment in stuck)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Pending → Failed is a legal transition. The exit is deliberately NOT
                // retriable: the charge outcome at the gateway is unknown, so an automated
                // re-charge could double-charge. An operator reconciles with the provider
                // (Failed → Pending is the retry path when the charge never happened).
                payment.MarkAsFailed(
                    $"Payment outcome was never observed by the gateway path and no webhook resolved it within {settings.StuckPendingTimeoutHours} hours; reconciled by the pending-payment aging sweep. Verify the charge with the provider before retrying.",
                    errorCode: FailureCode,
                    nextRetryAt: null,
                    isRetriable: false);
                await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
                exited++;

                logger.LogWarning(
                    "Aged outcome-unknown payment {PaymentId} (provider {Provider}) out of Pending after {Hours} hours; marked failed with reason for operator reconciliation",
                    payment.Id,
                    payment.Provider,
                    settings.StuckPendingTimeoutHours);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // One payment must never abort the sweep: it stays Pending and is
                // reconsidered on the next cycle.
                logger.LogError(exception,
                    "Pending-payment aging exit failed for payment {PaymentId}.",
                    payment.Id);
            }
        }

        if (stuck.Count > 0)
        {
            logger.LogInformation(
                "Pending-payment aging sweep exited {Exited} of {Stuck} stuck payment(s).",
                exited,
                stuck.Count);
        }

        return exited;
    }
}

/// <summary>
///     Hosted service polling for outcome-unknown payments stuck in Pending past the
///     configured timeout (issue #1202 gaps N6). Opt-in via <c>Payments:Aging:Enabled</c>
///     (safe default: disabled, matching the sibling retry-queue worker); default poll
///     interval hourly, default stuck timeout 48 hours.
/// </summary>
public sealed class PendingPaymentAgingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentAgingOptions> agingOptions,
    ILogger<PendingPaymentAgingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = agingOptions.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation(
                "PendingPaymentAgingBackgroundService is disabled (Payments:Aging:Enabled=false); outcome-unknown stuck-Pending payments stay untouched.");
            return;
        }

        var pollInterval = TimeSpan.FromMinutes(Math.Max(1, settings.PollIntervalMinutes));
        logger.LogInformation(
            "PendingPaymentAgingBackgroundService started with a {Interval} poll interval and a {Timeout}-hour stuck-Pending timeout.",
            pollInterval,
            settings.StuckPendingTimeoutHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IPendingPaymentAgingWorker>()
                    .ExitStuckPendingPaymentsAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Pending-payment aging sweep cycle failed");
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
