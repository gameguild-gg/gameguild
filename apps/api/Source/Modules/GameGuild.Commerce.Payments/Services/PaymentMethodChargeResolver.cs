namespace GameGuild.Commerce.Payments;

/// <summary>
///     Default-first charge-method resolution with fallback (issue #414). The charge path
///     resolves the owner's default saved method first; when that is absent or not usable
///     (expired, gateway-failed, removed), it falls back to the next usable saved method in
///     registry order (default-first, then most recently added).
/// </summary>
public interface IPaymentMethodChargeResolver
{
    /// <summary>
    ///     Resolves the gateway payment method ids to attempt for a charge, best candidate
    ///     first. Only usable (<see cref="PaymentMethodStatus.Active"/>) saved methods are
    ///     returned.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">Owner whose saved methods may be charged.</param>
    /// <param name="recordedGatewayPaymentMethodId">
    ///     The gateway reference the bill was recorded with, if any. When it is still a
    ///     usable saved method it is preferred first (explicit per-bill choice); otherwise
    ///     resolution is default-first.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered charge candidates (plaintext gateway ids), best first; empty when no usable saved method exists.</returns>
    Task<IReadOnlyList<string>> ResolveChargeCandidatesAsync(
        Guid tenantId,
        Guid userId,
        string? recordedGatewayPaymentMethodId,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PaymentMethodChargeResolver(
    IPaymentMethodRepository repository,
    IGatewayPaymentMethodIdProtector? gatewayIdProtector = null) : IPaymentMethodChargeResolver
{
    private readonly IGatewayPaymentMethodIdProtector _protector =
        gatewayIdProtector ?? PassthroughGatewayPaymentMethodIdProtector.Instance;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ResolveChargeCandidatesAsync(
        Guid tenantId,
        Guid userId,
        string? recordedGatewayPaymentMethodId,
        CancellationToken cancellationToken = default)
    {
        // Registry order is already default-first, then most recently created.
        var usable = await repository
            .ListAsync(tenantId, userId, cancellationToken)
            .ConfigureAwait(false);

        List<PaymentMethod> candidates = [.. usable.Where(method => method.IsChargeable)];

        // An explicit, still-usable recorded method keeps priority (per-bill choice);
        // otherwise the default (registry order) leads.
        if (!string.IsNullOrWhiteSpace(recordedGatewayPaymentMethodId))
        {
            var recorded = candidates.FirstOrDefault(IsRecorded);
            if (recorded is not null && candidates.Count > 1)
            {
                candidates.Remove(recorded);
                candidates.Insert(0, recorded);
            }

            bool IsRecorded(PaymentMethod method)
                => string.Equals(
                    _protector.Unprotect(method.GatewayPaymentMethodId),
                    recordedGatewayPaymentMethodId.Trim(),
                    StringComparison.Ordinal);
        }

        return candidates
            .Select(method => _protector.Unprotect(method.GatewayPaymentMethodId))
            .ToList();
    }
}
