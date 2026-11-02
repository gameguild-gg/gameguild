using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Braintree processing adapter (issue #413): implements <see cref="IPaymentGateway" />
///     for the charge, refund and inquiry paths through the Braintree server-to-server REST
///     API, plus the credential-validating connection test. Charges payment method nonces
///     (the client-side tokenization product of the Braintree JS SDK). Fail-closed by
///     construction: disabled until <c>PaymentGateways:Braintree:IsEnabled</c> opts in,
///     defaults to the sandbox endpoint, answers every operation of an
///     unconfigured/real-mode-disabled adapter with an explicit failure (never a fake
///     success). Failure classification mirrors the PayPal adapter: transport failures and
///     HTTP 408/429/5xx are outcome-unknown; a missing transaction id does not prove no charge occurred. Processor declines
///     and 4xx answers are terminal; anything that might have moved money but cannot be
///     confirmed (settlement-pending after a successful sale) is outcome-unknown.
/// </summary>
public sealed class BraintreePaymentGateway(
    IOptions<BraintreeGatewayOptions> options,
    IBraintreeApiClient apiClient) : IPaymentGateway, IGatewayConnectionTester
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.Ordinal)
    {
        "processor_declined",
        "gateway_rejected",
        "failed",
        "voided"
    };

    private static readonly HashSet<string> SuccessStatuses = new(StringComparer.Ordinal)
    {
        "submitted_for_settlement",
        "settling",
        "settled"
    };

    private readonly BraintreeGatewayOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.Braintree;

    /// <inheritdoc />
    public string DisplayName => "Braintree";

    /// <inheritdoc />
    public bool IsEnabled => _options.IsEnabled;

    /// <inheritdoc />
    public async Task<GatewayPaymentResult> ProcessPaymentAsync(
        GatewayPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return NotEnabledFailure();
        }

        if (_options.UseSimulation)
        {
            return SimulatedCharge();
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            return new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: "braintree_payment_method_required",
                ErrorMessage: "The Braintree adapter charges payment method nonces only; a payment method id is required.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline);
        }

        var apiResult = await apiClient.SaleAsync(
            request.IdempotencyKey,
            request.Amount,
            request.PaymentMethodId,
            request.Description,
            cancellationToken).ConfigureAwait(false);

        return MapChargeResult(apiResult);
    }

    /// <inheritdoc />
    public async Task<GatewayPaymentResult> GetPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default,
        string? providerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalTransactionId);

        if (!IsEnabled)
        {
            return NotEnabledFailure();
        }

        if (_options.UseSimulation)
        {
            return new GatewayPaymentResult(
                Success: true,
                TransactionId: externalTransactionId,
                ExternalPaymentId: externalTransactionId,
                ErrorCode: null,
                ErrorMessage: null,
                Status: PaymentStatus.Succeeded,
                ProcessedAt: SystemClock.UtcNow,
                ProviderMapping: CreateProviderMapping(externalTransactionId));
        }

        var apiResult = await apiClient.GetTransactionAsync(externalTransactionId, cancellationToken).ConfigureAwait(false);
        if (!apiResult.Success)
        {
            return MapChargeResult(apiResult);
        }

        var status = ExtractTransactionStatus(apiResult.Body);
        return new GatewayPaymentResult(
            Success: SuccessStatuses.Contains(status ?? string.Empty),
            TransactionId: externalTransactionId,
            ExternalPaymentId: externalTransactionId,
            ErrorCode: null,
            ErrorMessage: status is null ? "The Braintree transaction status could not be read." : null,
            Status: MapStatus(status),
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(externalTransactionId));
    }

    /// <inheritdoc />
    public Task<GatewayPaymentCancellationResult> CancelPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default,
        string? providerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalTransactionId);

        if (!IsEnabled)
        {
            return Task.FromResult(new GatewayPaymentCancellationResult(
                Success: false,
                OutcomeUnknown: false,
                ErrorCode: "braintree_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage));
        }

        if (_options.UseSimulation)
        {
            return Task.FromResult(new GatewayPaymentCancellationResult(true, OutcomeUnknown: false, null, null));
        }

        // Braintree voids only apply to transactions that have not settled yet; a settled
        // transaction must be refunded instead. The adapter reports cancellation as
        // accepted-but-unverified so the caller reconciles the authoritative state.
        return Task.FromResult(new GatewayPaymentCancellationResult(
            Success: true,
            OutcomeUnknown: true,
            ErrorCode: null,
            ErrorMessage: "Braintree cancellations must be reconciled: unsettled transactions void; settled transactions refund."));
    }

    /// <inheritdoc />
    public async Task<GatewayRefundResult> ProcessRefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return new GatewayRefundResult(
                Success: false,
                RefundId: null,
                AmountRefunded: 0,
                ErrorCode: "braintree_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage,
                ProcessedAt: SystemClock.UtcNow);
        }

        if (_options.UseSimulation)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: $"BT-REFUND-SIMULATED-{Guid.NewGuid():N}",
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        var apiResult = await apiClient.RefundAsync(
            request.OriginalTransactionId,
            request.Amount,
            cancellationToken).ConfigureAwait(false);

        if (apiResult.Success)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: ExtractTransactionId(apiResult.Body) ?? request.OriginalTransactionId,
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        return new GatewayRefundResult(
            Success: false,
            RefundId: null,
            AmountRefunded: 0,
            ErrorCode: apiResult.ErrorCode ?? "braintree_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Braintree refund failed.",
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: ClassifyFailure(apiResult));
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        // Braintree webhooks carry a bt_signature/bt_payload pair verified against the
        // gateway's public key; that verification is owned by the Commerce.Billing webhook
        // ingress. The processing adapter fails closed here.
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new GatewayCustomerResult(
            Success: false,
            ExternalCustomerId: null,
            ErrorCode: "braintree_customer_management_not_supported",
            ErrorMessage: "Braintree customer management is not wired to this adapter; use the Braintree customer API."));
    }

    /// <inheritdoc />
    public Task<GatewayPaymentMethodResult> CreatePaymentMethodAsync(
        GatewayPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new GatewayPaymentMethodResult(
            Success: false,
            ExternalPaymentMethodId: null,
            CardLast4: null,
            CardBrand: null,
            ExpiryMonth: null,
            ExpiryYear: null,
            ErrorCode: "braintree_payment_method_management_not_supported",
            ErrorMessage: "Braintree payment method nonces are created client-side; the adapter stores them as-is."));
    }

    /// <inheritdoc />
    public Task<GatewayDisputeActionResult> AcceptDisputeAsync(
        GatewayDisputeActionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(DisputeActionsNotSupported());

    /// <inheritdoc />
    public Task<GatewayDisputeActionResult> ChallengeDisputeAsync(
        GatewayDisputeChallengeRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(DisputeActionsNotSupported());

    /// <inheritdoc />
    public Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubscriptionId);

        return Task.FromResult(new GatewayCancellationResult(
            Success: false,
            ErrorCode: "braintree_subscription_management_not_supported",
            ErrorMessage: "Braintree subscription management is not wired to this adapter.",
            EffectiveDate: null));
    }

    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = SystemClock.UtcNow;

        if (!_options.IsEnabled)
        {
            return TestFailed("adapter_disabled", "The Braintree adapter is disabled; no credentials were exercised.", 0, checkedAtUtc);
        }

        if (_options.UseSimulation)
        {
            return new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "simulation",
                LatencyMs: 0,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }

        var startedAt = SystemClock.UtcNow;
        var apiResult = await apiClient.CreateClientTokenAsync(cancellationToken).ConfigureAwait(false);
        var latencyMs = (long)(SystemClock.UtcNow - startedAt).TotalMilliseconds;

        return apiResult.Success
            ? new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "live",
                LatencyMs: latencyMs,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc)
            : TestFailed(
                apiResult.ErrorCode ?? "braintree_connection_failed",
                apiResult.ErrorMessage ?? "The Braintree client-token probe failed.",
                latencyMs,
                checkedAtUtc);
    }

    private const string AdapterDisabledMessage =
        "The Braintree adapter is disabled (PaymentGateways:Braintree:IsEnabled is false); fail closed.";

    private GatewayPaymentResult NotEnabledFailure() =>
        new(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: "braintree_adapter_disabled",
            ErrorMessage: AdapterDisabledMessage,
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: GatewayFailureKind.TerminalDecline);

    private GatewayPaymentResult SimulatedCharge()
    {
        var transactionId = $"BT-SIMULATED-{Guid.NewGuid():N}";
        return new GatewayPaymentResult(
            Success: true,
            TransactionId: transactionId,
            ExternalPaymentId: transactionId,
            ErrorCode: null,
            ErrorMessage: null,
            Status: PaymentStatus.Succeeded,
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(transactionId));
    }

    private GatewayProviderMapping CreateProviderMapping(string objectId) =>
        new(
            _options.UseSimulation ? "test" : _options.EnvironmentLabel,
            _options.UseSimulation ? "acct_simulated_braintree" : $"braintree_{_options.MerchantId}",
            objectId,
            "transaction",
            "sale");

    private GatewayPaymentResult MapChargeResult(GatewayProviderApiResult apiResult)
    {
        if (apiResult.Success)
        {
            var transactionId = ExtractTransactionId(apiResult.Body);
            var status = ExtractTransactionStatus(apiResult.Body);

            if (transactionId is null)
            {
                return new GatewayPaymentResult(
                    Success: false,
                    TransactionId: null,
                    ExternalPaymentId: null,
                    ErrorCode: "braintree_transaction_not_confirmed",
                    ErrorMessage: "The Braintree response did not contain a transaction id; the outcome needs reconciliation.",
                    Status: PaymentStatus.Processing,
                    ProcessedAt: SystemClock.UtcNow,
                    FailureKind: GatewayFailureKind.OutcomeUnknown);
            }

            if (SuccessStatuses.Contains(status ?? string.Empty))
            {
                // submitted_for_settlement: the sale was accepted and settlement is in
                // progress — a confirmed provider object, so the charge is a success by
                // the adapter contract (settlement completes provider-side).
                return new GatewayPaymentResult(
                    Success: true,
                    TransactionId: transactionId,
                    ExternalPaymentId: transactionId,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Status: PaymentStatus.Succeeded,
                    ProcessedAt: SystemClock.UtcNow,
                    ProviderMapping: CreateProviderMapping(transactionId));
            }

            return new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: $"braintree_status_{status ?? "unknown"}",
                ErrorMessage: $"The Braintree transaction landed in status {status ?? "(unknown)"}.",
                Status: MapStatus(status),
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: TerminalStatuses.Contains(status ?? string.Empty)
                    ? GatewayFailureKind.TerminalDecline
                    : GatewayFailureKind.OutcomeUnknown);
        }

        var failureKind = apiResult.TransportFailed || apiResult.IsServerError
            ? GatewayFailureKind.OutcomeUnknown : GatewayFailureKind.TerminalDecline;
        return new GatewayPaymentResult(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: apiResult.ErrorCode ?? "braintree_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Braintree request failed.",
            Status: failureKind == GatewayFailureKind.OutcomeUnknown ? PaymentStatus.Processing : PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: failureKind);
    }

    private static GatewayFailureKind ClassifyFailure(GatewayProviderApiResult apiResult) =>
        apiResult.TransportFailed || apiResult.IsServerError
            ? GatewayFailureKind.RetriableGatewayFailure
            : GatewayFailureKind.TerminalDecline;

    private GatewayConnectionTestResult TestFailed(
        string errorCode,
        string errorMessage,
        long latencyMs,
        DateTime checkedAtUtc) =>
        new(ProviderId, Succeeded: false, Mode: "live", LatencyMs: latencyMs, ErrorCode: errorCode, ErrorMessage: errorMessage, CheckedAtUtc: checkedAtUtc);

    private static GatewayDisputeActionResult DisputeActionsNotSupported() => new(
        Success: false,
        ErrorCode: "braintree_dispute_actions_unsupported",
        ErrorMessage: "Braintree dispute actions are not implemented on this adapter; manage the dispute in the Braintree console.");

    private static PaymentStatus MapStatus(string? status) =>
        status switch
        {
            "authorized" => PaymentStatus.Processing,
            "submitted_for_settlement" or "settling" or "settled" => PaymentStatus.Succeeded,
            "voided" => PaymentStatus.Cancelled,
            "processor_declined" or "gateway_rejected" or "failed" => PaymentStatus.Failed,
            _ => PaymentStatus.Processing
        };

    private static string? ExtractTransactionId(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return XDocument.Parse(body).Descendants("transaction")
                .FirstOrDefault()?.Element("id")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? ExtractTransactionStatus(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return XDocument.Parse(body).Descendants("transaction")
                .FirstOrDefault()?.Element("status")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }
}
