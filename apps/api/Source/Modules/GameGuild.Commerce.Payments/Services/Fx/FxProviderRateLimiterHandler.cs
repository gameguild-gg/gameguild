using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Outbound FX provider rate limiter (issue #412 spec H): a per-provider sliding window
///     that caps how many HTTP calls the FX HttpClient may issue per minute. The sync
///     treats a breach as provider unavailability — the request is not sent, the provider is
///     skipped and the fallback takes over — so a runaway loop can never hammer a provider
///     account or burn its request quota. Disabled when
/// <see cref="FxSyncOptions.ProviderMaxCallsPerMinute"/> is 0.
/// </summary>
public sealed class FxProviderRateLimiterHandler(IOptions<FxSyncOptions> options) : DelegatingHandler
{
    private readonly object _windowLock = new();
    private readonly Queue<DateTime> _callTimestamps = new();

    /// <summary>
    ///     Records one permitted call, evicting timestamps that left the window.
    ///     Internal seam used by the HTTP pipeline and by unit tests.
    /// </summary>
    /// <param name="utcNow">Current instant.</param>
    /// <param name="maxCalls">Window capacity (0 disables the limiter).</param>
    /// <param name="window">Window length.</param>
    /// <returns>True when the call is permitted; false when the window is exhausted.</returns>
    internal bool TryAcquire(DateTime utcNow, int maxCalls, TimeSpan window)
    {
        if (maxCalls <= 0)
        {
            return true;
        }

        lock (_windowLock)
        {
            var cutoff = utcNow - window;
            while (_callTimestamps.Count > 0 && _callTimestamps.Peek() < cutoff)
            {
                _callTimestamps.Dequeue();
            }

            if (_callTimestamps.Count >= maxCalls)
            {
                return false;
            }

            _callTimestamps.Enqueue(utcNow);
            return true;
        }
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = options.Value;
        if (!TryAcquire(DateTime.UtcNow, settings.ProviderMaxCallsPerMinute, TimeSpan.FromMinutes(1)))
        {
            throw new FxRateProviderUnavailableException(
                "fx-http",
                $"FX provider rate limit reached: at most {settings.ProviderMaxCallsPerMinute} calls per minute are issued per provider "
                + "(FxRates:ProviderMaxCallsPerMinute). The request was not sent.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
