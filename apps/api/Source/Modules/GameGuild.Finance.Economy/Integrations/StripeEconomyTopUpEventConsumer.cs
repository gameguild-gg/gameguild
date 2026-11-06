using System.Security.Cryptography;
using System.Text;
using GameGuild.Commerce.Billing;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Funding;
using GameGuild.Finance.Economy.Ledger;
using GameGuild.Finance.Economy.Risk;

namespace GameGuild.Finance.Economy.Integrations;

public sealed class StripeEconomyTopUpEventConsumer(
    IEconomyTopUpSettlementStore topUps,
    IStripeEconomyFundingAdapter fundingAdapter,
    IHardCoinFundingGateway fundingGateway,
    IEconomyTrustedProtectedOperationAuthorizer protectedOperations,
    IRegisteredPostingCapabilityResolver capabilities,
    IProviderReversalGateway reversalGateway,
    TimeProvider timeProvider) : IStripeVerifiedEventConsumer
{
    internal const string RegisteredCapabilityName = "economy.confirm-hard-coin-funding.v1";

    /// <summary>
    ///     Registered posting capability authorizing persisted provider-funding reversals
    ///     (issue #1202 gaps N4): seeded by the AddEconomyTopUpRefundReversalCapability
    ///     migration and required by the economy_private.post_provider_reversal_v2 writer.
    /// </summary>
    internal const string RegisteredReversalCapabilityName = "economy.reverse-provider-funding.v1";

    /// <summary>
    ///     The cumulative-authoritative refund event: the charge object carries
    ///     <c>amount_refunded</c>, the running total across every refund of the intent.
    /// </summary>
    internal const string RefundReversalEventType = "charge.refunded";

    public async ValueTask<bool> TryConsumeAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(verifiedEvent);
        var status = MapStatus(verifiedEvent.EventType);
        if (!status.HasValue)
        {
            // Issue #1202 (gaps N4): refund events for a top-up payment intent used to be
            // released with the mint left intact. The cumulative charge.refunded event now
            // reverses the minted HardCoin units through the module's provider-reversal path.
            if (string.Equals(verifiedEvent.EventType, RefundReversalEventType, StringComparison.Ordinal))
            {
                return await TryReverseRefundedTopUpAsync(verifiedEvent, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        Validate(verifiedEvent);
        var identity = new EconomyTopUpProviderIdentity(
            "stripe",
            verifiedEvent.ProviderEnvironment,
            verifiedEvent.ProviderAccountId,
            verifiedEvent.ProviderObjectId,
            "payment_intent",
            "capture");
        var context = await topUps.FindAsync(identity, cancellationToken).ConfigureAwait(false);
        if (context is null)
        {
            return false;
        }

        EnsureAuthoritativePayment(verifiedEvent, context);

        if (status != EconomyTopUpProviderStatus.Posted)
        {
            await topUps.ApplyAsync(
                ProviderEvent(verifiedEvent, identity, status.Value),
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        if (context.TopUp.Status is EconomyTopUpProviderStatus.Posted or EconomyTopUpProviderStatus.Cancelled)
        {
            return true;
        }

        var evidence = $"stripe-event:{verifiedEvent.EventId}:sha256:{verifiedEvent.PayloadSha256}";
        var observation = fundingAdapter.CreateObservation(
            context.Payment,
            context.TopUp.WalletId,
            evidence,
            verifiedEvent.OccurredAt);
        var confirmedAt = timeProvider.GetUtcNow();
        var intent = new EconomyProtectedOperationIntent(
            EconomyValueMovementCapability.ConfirmHardCoinFunding,
            PostingTemplateKind.ConfirmedTopUpMint,
            context.TopUp.WalletId,
            context.TopUp.WalletId,
            new CoinAmount(CurrencyCode.HardCoin, context.TopUp.HardCoinUnits),
            [new RiskCurrencyLeg(CurrencyCode.HardCoin, context.TopUp.HardCoinUnits)],
            [observation.SourceId],
            Hash(identity),
            Hash(context.TopUp.WalletId.Value.ToString("N")),
            fundingAdapter.ConfirmationIdempotencyKey(context.Payment),
            confirmedAt,
            ProtectedSubjectId: context.TopUp.ActorId);

        try
        {
            _ = await protectedOperations.ExecuteAsync(
                context.TopUp.TenantId,
                context.TopUp.ActorId,
                intent,
                async (authorization, token) =>
                {
                    var claim = fundingGateway.Observe(new PersistedHardCoinFundingObservation(
                        observation,
                        authorization.ActorId,
                        authorization.TenantId,
                        new PolicyVersion(authorization.Receipt.PolicyVersion)));
                    var authority = await capabilities.ResolveAuthorityAsync(
                        RegisteredCapabilityName,
                        PostingTemplateKind.ConfirmedTopUpMint,
                        authorization.Receipt,
                        token).ConfigureAwait(false);
                    var confirmation = fundingAdapter.CreateDurableConfirmation(
                        context.Payment,
                        claim,
                        authorization,
                        authority,
                        evidence,
                        confirmedAt);
                    var receipt = fundingGateway.ConfirmDurable(confirmation);
                    await topUps.ApplyAsync(
                        ProviderEvent(
                            verifiedEvent,
                            identity,
                            EconomyTopUpProviderStatus.Posted,
                            receipt.PostingId.Value),
                        token).ConfigureAwait(false);
                    return receipt;
                },
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await topUps.ApplyAsync(
                ProviderEvent(verifiedEvent, identity, EconomyTopUpProviderStatus.Held),
                cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    internal static EconomyTopUpProviderStatus? MapStatus(string eventType) => eventType switch
    {
        "payment_intent.processing" => EconomyTopUpProviderStatus.Processing,
        "payment_intent.requires_action" => EconomyTopUpProviderStatus.RequiresAction,
        "payment_intent.succeeded" => EconomyTopUpProviderStatus.Posted,
        "payment_intent.payment_failed" => EconomyTopUpProviderStatus.Failed,
        "payment_intent.canceled" => EconomyTopUpProviderStatus.Cancelled,
        _ => null
    };

    /// <summary>
    ///     Reverses the HardCoin mint of a refunded top-up (issue #1202 gaps N4). The verified
    ///     <c>charge.refunded</c> event resolves to the same canonical payment-intent identity
    ///     as the mint, and its <c>CumulativeRefundedAmount</c> is the authoritative running
    ///     total the provider has returned to the customer. The reversal executes through the
    ///     module's own persisted provider-reversal path — the same writer the dispute loss
    ///     flow uses — under a protected operation keyed by the deterministic
    ///     <c>economy:stripe:reverse</c> idempotency, so webhook redeliveries and cumulative
    ///     step-ups deduplicate at the posting level and already-spent units partition into
    ///     wallet debt instead of vanishing.
    ///     The event is always RELEASED afterwards (<c>false</c>): the Commerce billing
    ///     lifecycle consumer still owns completing the local refund row from the same event.
    /// </summary>
    private async ValueTask<bool> TryReverseRefundedTopUpAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken)
    {
        Validate(verifiedEvent);
        var identity = new EconomyTopUpProviderIdentity(
            "stripe",
            verifiedEvent.ProviderEnvironment,
            verifiedEvent.ProviderAccountId,
            verifiedEvent.ProviderObjectId,
            "payment_intent",
            "capture");
        var context = await topUps.FindAsync(identity, cancellationToken).ConfigureAwait(false);
        if (context is null)
        {
            // Not an Economy top-up (a regular Commerce payment): nothing to reverse here.
            return false;
        }

        EnsureAuthoritativePayment(verifiedEvent, context);

        if (context.TopUp.Status is not EconomyTopUpProviderStatus.Posted)
        {
            // Nothing was minted (or the intent already reached another terminal state), so
            // there is no mint to reverse; release the event to the sibling consumers.
            return false;
        }

        if (verifiedEvent.CumulativeRefundedAmount is not > 0m)
        {
            // A zero cumulative refund cannot drive a reversal (defensive: the verifier only
            // populates the field when the provider object carries amount_refunded).
            return false;
        }

        var cumulativeRefunded = verifiedEvent.CumulativeRefundedAmount.Value;
        var cumulativeHardUnits = ToHardCoinUnits(cumulativeRefunded);
        var templateKind = cumulativeHardUnits >= context.TopUp.HardCoinUnits
            ? PostingTemplateKind.ProviderReversalFull
            : PostingTemplateKind.ProviderReversalPartial;

        var evidence = $"stripe-refund-event:{verifiedEvent.EventId}:sha256:{verifiedEvent.PayloadSha256}";
        var observation = fundingAdapter.CreateObservation(
            context.Payment,
            context.TopUp.WalletId,
            evidence,
            verifiedEvent.OccurredAt);
        var requestedAt = timeProvider.GetUtcNow();
        var intent = new EconomyProtectedOperationIntent(
            EconomyValueMovementCapability.ReverseProviderFunding,
            templateKind,
            context.TopUp.WalletId,
            context.TopUp.WalletId,
            new CoinAmount(CurrencyCode.HardCoin, cumulativeHardUnits),
            [new RiskCurrencyLeg(CurrencyCode.HardCoin, cumulativeHardUnits)],
            [observation.SourceId],
            Hash(identity),
            Hash(context.TopUp.WalletId.Value.ToString("N")),
            fundingAdapter.ReversalIdempotencyKey(context.Payment, cumulativeHardUnits),
            requestedAt,
            ProtectedSubjectId: context.TopUp.ActorId);

        _ = await protectedOperations.ExecuteAsync(
            context.TopUp.TenantId,
            context.TopUp.ActorId,
            intent,
            async (authorization, token) =>
            {
                var authority = await capabilities.ResolveAuthorityAsync(
                    RegisteredReversalCapabilityName,
                    templateKind,
                    authorization.Receipt,
                    token).ConfigureAwait(false);
                var command = fundingAdapter.CreateReversal(
                    context.Payment,
                    cumulativeRefunded,
                    cumulativeDisputedAmount: 0m,
                    // The customer received the money back from the provider; units already
                    // spent by the wallet become its debt rather than a platform loss.
                    ProviderReversalDisposition.ResponsibleDebt,
                    evidence,
                    reserveVersion: new ReserveVersion(authorization.Receipt.ReserveVersion),
                    policyVersion: new PolicyVersion(authorization.Receipt.PolicyVersion),
                    occurredAt: verifiedEvent.OccurredAt);
                return reversalGateway.Reverse(new PersistedProviderReversal(command, authority));
            },
            cancellationToken).ConfigureAwait(false);

        // Released on purpose: the Commerce billing lifecycle consumer completes the local
        // refund row from this same event; the ledger reversal above is idempotent, so a
        // later redelivery cannot double-reverse.
        return false;
    }

    /// <summary>
    ///     Converts a cumulative USD major-unit refund total to HardCoin units with the exact
    ///     minor-unit parity the funding adapter enforces (HardCoin units equal authoritative
    ///     USD minor units); fractions that cannot be represented fail closed.
    /// </summary>
    private static long ToHardCoinUnits(decimal cumulativeRefunded)
    {
        var scaled = checked(cumulativeRefunded * 100m);
        if (scaled != decimal.Truncate(scaled) || scaled <= 0 || scaled > long.MaxValue)
        {
            throw new InvalidOperationException(
                "A refunded top-up total must be exactly representable in whole HardCoin units.");
        }

        return decimal.ToInt64(scaled);
    }

    internal static void Validate(VerifiedStripeWebhookEvent verifiedEvent)
    {
        if (verifiedEvent.OccurredAt == default ||
            string.IsNullOrWhiteSpace(verifiedEvent.EventId) ||
            string.IsNullOrWhiteSpace(verifiedEvent.ProviderEnvironment) ||
            string.IsNullOrWhiteSpace(verifiedEvent.ProviderAccountId) ||
            string.IsNullOrWhiteSpace(verifiedEvent.ProviderObjectId) ||
            !string.Equals(verifiedEvent.ProviderObjectType, "payment_intent", StringComparison.Ordinal) ||
            verifiedEvent.PayloadSha256.Length != 64 ||
            !verifiedEvent.PayloadSha256.All(Uri.IsHexDigit))
        {
            throw new InvalidWebhookPayloadException(
                "Stripe top-up event identity or evidence is incomplete.");
        }
    }

    internal static void EnsureAuthoritativePayment(
        VerifiedStripeWebhookEvent verifiedEvent,
        EconomyTopUpSettlementContext context)
    {
        if (!verifiedEvent.Amount.HasValue || verifiedEvent.Amount.Value != context.Payment.Amount ||
            !string.Equals(verifiedEvent.Currency, context.Payment.Currency, StringComparison.OrdinalIgnoreCase) ||
            verifiedEvent.TenantId.HasValue && verifiedEvent.TenantId.Value != context.TopUp.TenantId)
        {
            throw new InvalidWebhookPayloadException(
                "Stripe top-up event does not match the authoritative payment.");
        }
    }

    private static EconomyTopUpProviderEvent ProviderEvent(
        VerifiedStripeWebhookEvent verifiedEvent,
        EconomyTopUpProviderIdentity identity,
        EconomyTopUpProviderStatus status,
        Guid? postingGroupId = null) => new(
        identity,
        verifiedEvent.EventId,
        verifiedEvent.OccurredAt,
        status,
        verifiedEvent.PayloadSha256,
        decimal.ToInt64(verifiedEvent.Amount!.Value * 100m),
        verifiedEvent.Currency!.ToUpperInvariant(),
        postingGroupId,
        status switch
        {
            EconomyTopUpProviderStatus.Failed => "payment_failed",
            EconomyTopUpProviderStatus.Cancelled => "cancelled",
            _ => null
        });

    private static string Hash(EconomyTopUpProviderIdentity identity) => Hash(string.Join(
        '\u001f',
        identity.Provider,
        identity.ProviderEnvironment,
        identity.ProviderAccountId,
        identity.ProviderObjectId,
        identity.ProviderObjectType,
        identity.ProviderMonetaryLeg));

    private static string Hash(string value) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
