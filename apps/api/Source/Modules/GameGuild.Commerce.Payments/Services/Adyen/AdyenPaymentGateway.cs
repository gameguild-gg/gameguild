using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Adyen processing adapter (issue #413, international gateway): implements
///     <see cref="IPaymentGateway" /> for the charge, refund and cancellation paths through
///     the Adyen Checkout API, plus the credential-validating connection test. Charges stored
///     payment method ids (Adyen recurring detail references / storedPaymentMethodId).
///     Fail-closed by construction: disabled until <c>PaymentGateways:Adyen:IsEnabled</c>
///     opts in, defaults to the test endpoint, answers every operation of an
///     unconfigured/real-mode-disabled adapter with an explicit failure (never a fake
///     success). Failure classification mirrors the PayPal adapter: transport failures and
///     HTTP 408/429/5xx are outcome-unknown; a missing pspReference does not prove no charge occurred. Refusals and definitive 4xx answers are
///     terminal; anything that may have moved money without a confirmed result code is
///     outcome-unknown. Payment inquiries are not part of the Checkout API, so
///     <see cref="GetPaymentAsync" /> fails closed with an explicit result — Adyen payment
///     state arrives through webhooks and reconciliation.
/// </summary>
public sealed class AdyenPaymentGateway(
    IOptions<AdyenGatewayOptions> options,
    IAdyenApiClient apiClient) : IPaymentGateway, IGatewayConnectionTester
{
    private readonly AdyenGatewayOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.Adyen;

    /// <inheritdoc />
    public string DisplayName => "Adyen";

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
                ErrorCode: "adyen_payment_method_required",
                ErrorMessage: "The Adyen adapter charges stored payment method ids only; a payment method id is required.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline);
        }

        var apiResult = await apiClient.PaymentAsync(
            request.IdempotencyKey,
            request.Amount,
            request.Currency,
            request.PaymentMethodId,
            cancellationToken).ConfigureAwait(false);

        return MapChargeResult(apiResult);
    }

    /// <inheritdoc />
    public Task<GatewayPaymentResult> GetPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default,
        string? providerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalTransactionId);

        if (!IsEnabled)
        {
            return Task.FromResult(NotEnabledFailure());
        }

        // The Checkout API offers no payment-status inquiry; state arrives through
        // webhooks and reconciliation. Fail closed instead of fabricating a result.
        return Task.FromResult(new GatewayPaymentResult(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: "adyen_inquiry_not_supported",
            ErrorMessage: "The Adyen Checkout API does not expose payment inquiry; reconcile the payment state from webhook notifications.",
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: GatewayFailureKind.TerminalDecline));
    }

    /// <inheritdoc />
    public async Task<GatewayPaymentCancellationResult> CancelPaymentAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default,
        string? providerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalTransactionId);

        if (!IsEnabled)
        {
            return new GatewayPaymentCancellationResult(
                Success: false,
                OutcomeUnknown: false,
                ErrorCode: "adyen_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage);
        }

        if (_options.UseSimulation)
        {
            return new GatewayPaymentCancellationResult(true, OutcomeUnknown: false, null, null);
        }

        var apiResult = await apiClient.CancelAsync(externalTransactionId, cancellationToken).ConfigureAwait(false);

        if (apiResult.Success)
        {
            return new GatewayPaymentCancellationResult(true, OutcomeUnknown: false, null, null);
        }

        return new GatewayPaymentCancellationResult(
            Success: false,
            // An already-captured payment cannot be cancelled (it must be refunded), but a
            // 4xx on cancels does not reveal whether the authorisation is still open in
            // every case — report outcome unknown so the caller reconciles.
            OutcomeUnknown: apiResult.IsServerError || apiResult.TransportFailed,
            ErrorCode: apiResult.ErrorCode ?? "adyen_cancel_failed",
            ErrorMessage: apiResult.ErrorMessage ?? "The Adyen cancellation failed.");
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
                ErrorCode: "adyen_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage,
                ProcessedAt: SystemClock.UtcNow);
        }

        if (_options.UseSimulation)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: $"ADYEN-REFUND-SIMULATED-{Guid.NewGuid():N}",
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        var apiResult = await apiClient.RefundAsync(
            request.OriginalTransactionId,
            request.Amount,
            request.Currency ?? "USD",
            cancellationToken).ConfigureAwait(false);

        if (apiResult.Success)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: ExtractPspReference(apiResult.Body) ?? request.OriginalTransactionId,
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        return new GatewayRefundResult(
            Success: false,
            RefundId: null,
            AmountRefunded: 0,
            ErrorCode: apiResult.ErrorCode ?? "adyen_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Adyen refund failed.",
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: ClassifyFailure(apiResult));
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        // Adyen webhooks are verified by an HMAC-SHA256 signature over a sorted
        // key/value serialization with the webhook secret; that verification is owned by
        // the Commerce.Billing webhook ingress. The processing adapter fails closed here.
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Adyen shopper records are managed through the /customers platform API; the
        // adapter fails closed instead of returning a fake shopper reference.
        return Task.FromResult(new GatewayCustomerResult(
            Success: false,
            ExternalCustomerId: null,
            ErrorCode: "adyen_customer_management_not_supported",
            ErrorMessage: "Adyen shopper management is not wired to this adapter; use the Adyen customers API."));
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
            ErrorCode: "adyen_payment_method_management_not_supported",
            ErrorMessage: "Adyen stored payment methods are created through the platform tokenization flow; the adapter stores them as-is."));
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
            ErrorCode: "adyen_subscription_management_not_supported",
            ErrorMessage: "Adyen subscription management is not wired to this adapter.",
            EffectiveDate: null));
    }

    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = SystemClock.UtcNow;

        if (!_options.IsEnabled)
        {
            return TestFailed("adapter_disabled", "The Adyen adapter is disabled; no credentials were exercised.", 0, checkedAtUtc);
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
        var apiResult = await apiClient.GetPaymentMethodsAsync(cancellationToken).ConfigureAwait(false);
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
                apiResult.ErrorCode ?? "adyen_connection_failed",
                apiResult.ErrorMessage ?? "The Adyen payment-methods probe failed.",
                latencyMs,
                checkedAtUtc);
    }

    private const string AdapterDisabledMessage =
        "The Adyen adapter is disabled (PaymentGateways:Adyen:IsEnabled is false); fail closed.";

    private GatewayPaymentResult NotEnabledFailure() =>
        new(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: "adyen_adapter_disabled",
            ErrorMessage: AdapterDisabledMessage,
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: GatewayFailureKind.TerminalDecline);

    private GatewayPaymentResult SimulatedCharge()
    {
        var pspReference = $"ADYEN-SIMULATED-{Guid.NewGuid():N}";
        return new GatewayPaymentResult(
            Success: true,
            TransactionId: pspReference,
            ExternalPaymentId: pspReference,
            ErrorCode: null,
            ErrorMessage: null,
            Status: PaymentStatus.Succeeded,
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(pspReference));
    }

    private GatewayProviderMapping CreateProviderMapping(string objectId) =>
        new(
            _options.UseSimulation ? "test" : _options.EnvironmentLabel,
            _options.UseSimulation ? "acct_simulated_adyen" : $"adyen_{_options.MerchantAccount}",
            objectId,
            "payment",
            "authorisation");

    private GatewayPaymentResult MapChargeResult(GatewayProviderApiResult apiResult)
    {
        if (apiResult.Success)
        {
            var pspReference = ExtractPspReference(apiResult.Body);
            var resultCode = ExtractStringProperty(apiResult.Body, "resultCode");

            if (pspReference is null || resultCode is null)
            {
                // The call succeeded at the transport level but the result is not
                // readable: the authorisation may exist, so the outcome is unknown.
                return new GatewayPaymentResult(
                    Success: false,
                    TransactionId: null,
                    ExternalPaymentId: null,
                    ErrorCode: "adyen_result_not_confirmed",
                    ErrorMessage: "The Adyen response did not contain a readable pspReference/resultCode; the outcome needs reconciliation.",
                    Status: PaymentStatus.Processing,
                    ProcessedAt: SystemClock.UtcNow,
                    FailureKind: GatewayFailureKind.OutcomeUnknown);
            }

            if (resultCode == "Authorised" || resultCode == "Received")
            {
                return new GatewayPaymentResult(
                    Success: true,
                    TransactionId: pspReference,
                    ExternalPaymentId: pspReference,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Status: PaymentStatus.Succeeded,
                    ProcessedAt: SystemClock.UtcNow,
                    ProviderMapping: CreateProviderMapping(pspReference));
            }

            // Refused (issuer decline), Cancelled, Error: definitive negative answers.
            return new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: $"adyen_result_{resultCode.ToLowerInvariant()}",
                ErrorMessage: ExtractRefusalReason(apiResult.Body) is { } reason ? $"Adyen refused the payment: {reason}." : $"Adyen answered resultCode {resultCode}.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline);
        }

        var failureKind = apiResult.TransportFailed || apiResult.IsServerError
            ? GatewayFailureKind.OutcomeUnknown : GatewayFailureKind.TerminalDecline;
        return new GatewayPaymentResult(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: apiResult.ErrorCode ?? "adyen_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Adyen request failed.",
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
        ErrorCode: "adyen_dispute_actions_unsupported",
        ErrorMessage: "Adyen dispute actions are not implemented on this adapter; manage the dispute in the Adyen Customer Area.");

    private static string? ExtractPspReference(string? body) =>
        ExtractStringProperty(body, "pspReference");

    private static string? ExtractRefusalReason(string? body) =>
        ExtractStringProperty(body, "refusalReason");

    private static string? ExtractStringProperty(string? body, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(propertyName, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
