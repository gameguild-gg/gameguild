using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Cross-account device fingerprint signal provider (issue #409: detect the same
///     device across multiple accounts). When the attempt carries a device fingerprint,
///     looks back over the tenant's payment history for other customer accounts that
///     presented the same fingerprint; sharing across accounts raises the
///     <c>Device.CrossAccountUsage</c> signal.
/// </summary>
/// <remarks>
///     <para>
///         The look-back query matches the partial index
///         <c>ix_payments_fraud_device_lookup</c> (attempts that carried a fingerprint).
///         Unlike the velocity provider, fraud-rejected attempts on OTHER accounts are
///         intentionally included — a fingerprint already tied to a rejected attempt on a
///         different account is precisely the correlation this signal exists to catch.
///     </para>
///     <para>
///         Account scope mirrors the velocity provider: accounts are distinguished by the
///         external (provider) customer id, falling back to the subscription for attempts
///         not yet bound to a provider customer.
///     </para>
/// </remarks>
public sealed class DeviceFingerprintFraudSignalProvider(
    IApplicationDbContext context,
    ILogger<DeviceFingerprintFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string SignalType = "Device.CrossAccountUsage";

    /// <inheritdoc />
    public string Name => "DeviceFingerprint";

    /// <inheritdoc />
    public async Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalContext);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(signalContext.DeviceFingerprint))
        {
            return [];
        }

        var windowStart = SystemClock.UtcNow - TimeSpan.FromDays(options.CrossAccountDeviceLookbackDays);

        var accountKeys = await context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.TenantId == signalContext.TenantId
                              && payment.Id != signalContext.PaymentId
                              && payment.FraudDeviceFingerprint == signalContext.DeviceFingerprint
                              && payment.CreatedAt >= windowStart
                              && payment.Status != PaymentStatus.Cancelled)
            .Select(payment => new { payment.ExternalCustomerId, payment.SubscriptionId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // The account being charged is one of the accounts on this device; the signal
        // counts the DISTINCT accounts (including the current one) the device served.
        var currentAccountKey = signalContext.ExternalCustomerId ?? signalContext.SubscriptionId.ToString();
        var distinctAccounts = accountKeys
            .Select(attempt => attempt.ExternalCustomerId ?? attempt.SubscriptionId.ToString())
            .Append(currentAccountKey)
            .Distinct()
            .Count();

        var signals = new List<FraudSignal>();

        if (distinctAccounts >= options.CrossAccountDeviceMinAccounts)
        {
            var extraAccounts = distinctAccounts - options.CrossAccountDeviceMinAccounts;
            var weight = Math.Min(
                options.CrossAccountDeviceWeight + (extraAccounts * options.CrossAccountDeviceWeight),
                options.MaxSignalScore);
            signals.Add(new FraudSignal(
                SignalType,
                weight,
                JsonSerializer.Serialize(new
                {
                    deviceSharedByAccounts = distinctAccounts,
                    threshold = options.CrossAccountDeviceMinAccounts,
                    lookbackDays = options.CrossAccountDeviceLookbackDays
                })));
        }

        logger.LogDebug(
            "Fraud device-fingerprint evaluation for tenant {TenantId} payment {PaymentId}: device seen on {AccountCount} account(s) in the last {LookbackDays} day(s)",
            signalContext.TenantId,
            signalContext.PaymentId,
            distinctAccounts,
            options.CrossAccountDeviceLookbackDays);

        return signals;
    }
}
