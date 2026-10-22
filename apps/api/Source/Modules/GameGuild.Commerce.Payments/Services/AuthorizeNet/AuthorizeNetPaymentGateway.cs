using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Authorize.Net processing adapter (issue #413): implements <see cref="IPaymentGateway" />
///     for the charge, refund and inquiry paths through the Authorize.Net JSON API, plus the
///     credential-validating connection test. Fail-closed by construction: disabled until
///     <c>PaymentGateways:AuthorizeNet:IsEnabled</c> opts in, defaults to the sandbox endpoint,
///     answers every operation of an unconfigured/real-mode-disabled adapter with an explicit
///     failure (never a fake success), and only performs real HTTP calls when simulation is
///     off. Failure classification mirrors the PayPal adapter: transport failures and HTTP
///     408/429/5xx without a confirmed transaction are retriable; definitive 4xx answers (and
///     Authorize.Net declines, response codes 2/3) are terminal declines; anything that might
///     have moved money but cannot be confirmed is outcome-unknown and left to reconciliation.
/// </summary>
public sealed class AuthorizeNetPaymentGateway(
    IOptions<AuthorizeNetGatewayOptions> options,
    IAuthorizeNetApiClient apiClient) : IPaymentGateway, IGatewayConnectionTester
{
    private readonly AuthorizeNetGatewayOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.AuthorizeNet;

    /// <inheritdoc />
    public string DisplayName => "Authorize.Net";

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
                ErrorCode: "authorizenet_payment_method_required",
                ErrorMessage: "The Authorize.Net adapter charges stored opaque-data tokens only; a payment method id is required.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline);
        }

        var apiResult = await apiClient.ChargeAsync(
            request.IdempotencyKey,
            request.Amount,
            request.Currency,
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
                ProviderMapping: CreateProviderMapping(externalTransactionId, captured: true));
        }

        var apiResult = await apiClient.GetTransactionAsync(externalTransactionId, cancellationToken).ConfigureAwait(false);
        if (!apiResult.Success)
        {
            return MapChargeResult(apiResult);
        }

        var (statusCode, settled) = ExtractTransactionStatus(apiResult.Body);
        return new GatewayPaymentResult(
            Success: statusCode == "settledSuccessfully" || (statusCode == "capture" && settled),
            TransactionId: externalTransactionId,
            ExternalPaymentId: externalTransactionId,
            ErrorCode: null,
            ErrorMessage: statusCode is null ? "The Authorize.Net transaction status could not be read." : null,
            Status: statusCode switch
            {
                "settledSuccessfully" => PaymentStatus.Succeeded,
                "capturedPendingSettlement" or "capture" or "authorizedPendingCapture" or "underReview" => PaymentStatus.Processing,
                "voided" => PaymentStatus.Cancelled,
                "declined" or "genericDecline" or "fraudDetected" => PaymentStatus.Failed,
                _ => PaymentStatus.Failed
            },
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(externalTransactionId, captured: true));
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
                ErrorCode: "authorizenet_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage));
        }

        if (_options.UseSimulation)
        {
            return Task.FromResult(new GatewayPaymentCancellationResult(true, OutcomeUnknown: false, null, null));
        }

        // Authorize.Net voids happen through the voidTransaction type against unsettled
        // transactions; a settled transaction cannot be voided and must be refunded. The
        // adapter reports cancellation as accepted-but-unverified so the caller reconciles
        // the authoritative state instead of assuming the void landed.
        return Task.FromResult(new GatewayPaymentCancellationResult(
            Success: true,
            OutcomeUnknown: true,
            ErrorCode: null,
            ErrorMessage: "Authorize.Net cancellations must be reconciled: unsettled transactions void; settled transactions refund."));
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
                ErrorCode: "authorizenet_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage,
                ProcessedAt: SystemClock.UtcNow);
        }

        if (_options.UseSimulation)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: $"ANET-REFUND-SIMULATED-{Guid.NewGuid():N}",
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        var apiResult = await apiClient.RefundAsync(
            request.IdempotencyKey,
            request.OriginalTransactionId,
            request.Amount,
            cancellationToken).ConfigureAwait(false);

        if (apiResult.Success)
        {
            var refundId = ExtractTransactionId(apiResult.Body) ?? request.OriginalTransactionId;
            return new GatewayRefundResult(
                Success: true,
                RefundId: refundId,
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        return new GatewayRefundResult(
            Success: false,
            RefundId: null,
            AmountRefunded: 0,
            ErrorCode: apiResult.ErrorCode ?? "authorizenet_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Authorize.Net refund failed.",
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: ClassifyFailure(apiResult));
    }

    /// <inheritdoc />
    public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        // Authorize.Net webhooks are validated by SHA-512 digest of the body with the
        // signature header (an HMAC-less digest); that verification is owned by the
        // Commerce.Billing webhook ingress. The processing adapter fails closed here.
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Authorize.Net customer profiles are a separate API surface (customer profile
        // ids); the adapter fails closed instead of returning a fake external id.
        return Task.FromResult(new GatewayCustomerResult(
            Success: false,
            ExternalCustomerId: null,
            ErrorCode: "authorizenet_customer_management_not_supported",
            ErrorMessage: "Authorize.Net customer profiles are not wired to this adapter; use the customer profile API."));
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
            ErrorCode: "authorizenet_payment_method_management_not_supported",
            ErrorMessage: "Authorize.Net opaque-data tokens are created client-side by Accept.js; the adapter stores them as-is."));
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
            ErrorCode: "authorizenet_subscription_management_not_supported",
            ErrorMessage: "Authorize.Net ARB subscription management is not wired to this adapter.",
            EffectiveDate: null));
    }

    /// <inheritdoc />
    public async Task<GatewayConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var checkedAtUtc = SystemClock.UtcNow;

        if (!_options.IsEnabled)
        {
            return TestFailed("adapter_disabled", "The Authorize.Net adapter is disabled; no credentials were exercised.", 0, checkedAtUtc);
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
        var apiResult = await apiClient.GetMerchantDetailsAsync(cancellationToken).ConfigureAwait(false);
        var latencyMs = (long)(SystemClock.UtcNow - startedAt).TotalMilliseconds;

        if (apiResult.Success)
        {
            return new GatewayConnectionTestResult(
                ProviderId,
                Succeeded: true,
                Mode: "live",
                LatencyMs: latencyMs,
                ErrorCode: null,
                ErrorMessage: null,
                CheckedAtUtc: checkedAtUtc);
        }

        return TestFailed(
            apiResult.ErrorCode ?? "authorizenet_connection_failed",
            apiResult.ErrorMessage ?? "The Authorize.Net merchant-details probe failed.",
            latencyMs,
            checkedAtUtc);
    }

    private const string AdapterDisabledMessage =
        "The Authorize.Net adapter is disabled (PaymentGateways:AuthorizeNet:IsEnabled is false); fail closed.";

    private GatewayPaymentResult NotEnabledFailure() =>
        new(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: "authorizenet_adapter_disabled",
            ErrorMessage: AdapterDisabledMessage,
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: GatewayFailureKind.TerminalDecline);

    private GatewayPaymentResult SimulatedCharge()
    {
        var transactionId = $"ANET-SIMULATED-{Guid.NewGuid():N}";
        return new GatewayPaymentResult(
            Success: true,
            TransactionId: transactionId,
            ExternalPaymentId: transactionId,
            ErrorCode: null,
            ErrorMessage: null,
            Status: PaymentStatus.Succeeded,
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(transactionId, captured: true));
    }

    private GatewayProviderMapping CreateProviderMapping(string objectId, bool captured) =>
        new(
            _options.UseSimulation ? "test" : _options.EnvironmentLabel,
            _options.UseSimulation ? "acct_simulated_anet" : $"anet_{_options.ApiLoginId}",
            objectId,
            captured ? "capture" : "authorization",
            captured ? "capture" : "authorization");

    private GatewayPaymentResult MapChargeResult(GatewayProviderApiResult apiResult)
    {
        if (apiResult.Success)
        {
            var transactionId = ExtractTransactionId(apiResult.Body);
            var responseCode = ExtractTransactionResponseCode(apiResult.Body);

            if (transactionId is null)
            {
                // No provider transaction object was confirmed: outcome unknown.
                return new GatewayPaymentResult(
                    Success: false,
                    TransactionId: null,
                    ExternalPaymentId: null,
                    ErrorCode: "authorizenet_transaction_not_confirmed",
                    ErrorMessage: "The Authorize.Net response did not contain a transaction id; the outcome needs reconciliation.",
                    Status: PaymentStatus.Processing,
                    ProcessedAt: SystemClock.UtcNow,
                    FailureKind: GatewayFailureKind.OutcomeUnknown);
            }

            if (responseCode == "1")
            {
                return new GatewayPaymentResult(
                    Success: true,
                    TransactionId: transactionId,
                    ExternalPaymentId: transactionId,
                    ErrorCode: null,
                    ErrorMessage: null,
                    Status: PaymentStatus.Succeeded,
                    ProcessedAt: SystemClock.UtcNow,
                    ProviderMapping: CreateProviderMapping(transactionId, captured: true));
            }

            // Authorize.Net response codes: 2 = declined, 3 = error (e.g. expired card);
            // 4 = held for review. 2/3 are terminal declines; 4 is outcome-unknown (the
            // transaction exists and may be approved manually).
            return new GatewayPaymentResult(
                Success: false,
                TransactionId: null,
                ExternalPaymentId: null,
                ErrorCode: $"authorizenet_response_{responseCode ?? "unknown"}",
                ErrorMessage: ExtractFirstMessage(apiResult.Body) ?? $"Authorize.Net answered response code {responseCode}.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: responseCode == "4"
                    ? GatewayFailureKind.OutcomeUnknown
                    : GatewayFailureKind.TerminalDecline);
        }

        return new GatewayPaymentResult(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: apiResult.ErrorCode ?? "authorizenet_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The Authorize.Net request failed.",
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: ClassifyFailure(apiResult));
    }

    private static GatewayFailureKind ClassifyFailure(GatewayProviderApiResult apiResult)
    {
        if (apiResult.TransportFailed || apiResult.IsServerError)
        {
            // No confirmed provider payment object exists for a transport failure or an
            // HTTP 408/429/5xx answer: safe to retry (refId idempotency) and to fail over.
            return GatewayFailureKind.RetriableGatewayFailure;
        }

        return GatewayFailureKind.TerminalDecline;
    }

    private GatewayConnectionTestResult TestFailed(
        string errorCode,
        string errorMessage,
        long latencyMs,
        DateTime checkedAtUtc) =>
        new(ProviderId, Succeeded: false, Mode: "live", LatencyMs: latencyMs, ErrorCode: errorCode, ErrorMessage: errorMessage, CheckedAtUtc: checkedAtUtc);

    private static GatewayDisputeActionResult DisputeActionsNotSupported() => new(
        Success: false,
        ErrorCode: "authorizenet_dispute_actions_unsupported",
        ErrorMessage: "Authorize.Net dispute actions are not implemented on this adapter; manage the dispute in the Authorize.Net console.");

    private static string? ExtractStringProperty(string? body, params string[] path)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var element = document.RootElement;
            foreach (var segment in path)
            {
                if (!element.TryGetProperty(segment, out element))
                {
                    return null;
                }
            }

            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractTransactionId(string? body) =>
        ExtractStringProperty(body, "transactionResponse", "transId");

    private static string? ExtractTransactionResponseCode(string? body) =>
        ExtractStringProperty(body, "transactionResponse", "responseCode");

    private static string? ExtractFirstMessage(string? body) =>
        ExtractStringProperty(body, "messages", "message");

    private static (string? Status, bool Settled) ExtractTransactionStatus(string? body)
    {
        var status = ExtractStringProperty(body, "transaction", "transactionStatus");
        var settled = ExtractStringProperty(body, "transaction", "batch", "settlementState") == "settledSuccessfully";
        return (status, settled);
    }
}
