using GameGuild.Compliance.FinancialCrime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Hosted wrapper draining the FinancialCrime module's ingestion outbox on a bounded
///     interval (issue #1291 option C). Follows the <see cref="ComplianceDeadlineExpiryWorker"/>
///     shape: a fresh scope per cycle and retry on the next interval without crashing the host.
///     The worker logic itself lives in the module and is covered by its own suites.
/// </summary>
internal sealed class FinancialCrimeSignalOutboxWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<FinancialCrimeIngestionWorkerOptions> options,
    ILogger<FinancialCrimeSignalOutboxWorkerService> logger) : BackgroundService
{
    private readonly FinancialCrimeIngestionWorkerOptions _options =
        (options ?? throw new ArgumentNullException(nameof(options))).Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Financial Crime signal ingestion is disabled by configuration.");
            return;
        }

        try
        {
            await Task.Delay(_options.InitialDelay, stoppingToken).ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var worker = scope.ServiceProvider.GetRequiredService<IFinancialCrimeSignalIngestionWorker>();
                    await worker.ProcessDueAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception,
                        "Financial Crime signal ingestion pass failed; retrying on the next interval.");
                }

                await Task.Delay(_options.Interval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }
}

/// <summary>
///     Hosted wrapper running the FinancialCrime rescreening sweep on a bounded interval
///     (issue #1291 option C): due internal-provider screenings are re-synthesized through
///     <see cref="IFinancialCrimeControlPlane.ReadDueRescreeningsAsync"/> so a subject's
///     evidence never silently goes stale while activity continues.
/// </summary>
internal sealed class FinancialCrimeRescreeningWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<FinancialCrimeRescreeningWorkerOptions> options,
    ILogger<FinancialCrimeRescreeningWorkerService> logger) : BackgroundService
{
    private readonly FinancialCrimeRescreeningWorkerOptions _options =
        (options ?? throw new ArgumentNullException(nameof(options))).Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Financial Crime rescreening is disabled by configuration.");
            return;
        }

        try
        {
            await Task.Delay(_options.InitialDelay, stoppingToken).ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var worker = scope.ServiceProvider.GetRequiredService<IFinancialCrimeRescreeningWorker>();
                    await worker.RunAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception,
                        "Financial Crime rescreening pass failed; retrying on the next interval.");
                }

                await Task.Delay(_options.Interval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }
}
