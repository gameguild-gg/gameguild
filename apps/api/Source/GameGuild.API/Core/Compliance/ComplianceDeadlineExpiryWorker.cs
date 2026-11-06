using Microsoft.Extensions.Options;

namespace GameGuild.API.Core.Compliance;

/// <summary>
///     Bounded periodic background service (issue #1219 N3) that fires the statutory
///     deadlines the compliance modules only stored before: the GDPR 30-day data
///     subject request deadline and the FERPA 45-day inspection request deadline.
///     Follows the RefreshTokenCleanupWorker shape — a fresh scope per cycle, a linked
///     per-cycle timeout, and retry on the next interval without crashing the host.
/// </summary>
internal sealed class ComplianceDeadlineExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ComplianceDeadlineExpiryOptions> options,
    ILogger<ComplianceDeadlineExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var policy = options.Value;
        if (policy.Validate().Count != 0) { throw new InvalidOperationException("Invalid compliance deadline expiry configuration."); }
        if (!policy.Enabled) { return; }
        try
        {
            await Task.Delay(policy.InitialDelay, stoppingToken).ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                using var timeout = new CancellationTokenSource(policy.ExecutionTimeout);
                using var cycle = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var operation = scope.ServiceProvider.GetRequiredService<IComplianceDeadlineExpiryOperation>();
                    var result = await operation.RunAsync(cycle.Token).ConfigureAwait(false);
                    if (result.TotalExpired > 0)
                    {
                        logger.LogWarning(
                            "Compliance statutory deadlines expired. DataSubjectRequestsExpired: {DataSubjectRequestsExpired}, FerpaInspectionRequestsExpired: {FerpaInspectionRequestsExpired}",
                            result.DataSubjectRequestsExpired,
                            result.FerpaInspectionRequestsExpired);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    logger.LogWarning("Compliance deadline expiry reached its cycle deadline; retrying on the next interval.");
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Compliance deadline expiry failed; retrying on the next interval.");
                }
                await Task.Delay(policy.Interval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }
}
