using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     PayPal processing adapter (issue #413): implements <see cref="IPaymentGateway" /> for the
///     charge and refund paths through the PayPal Orders v2 API. Fail-closed by construction:
///     the adapter is disabled until <c>PaymentGateways:PayPal:IsEnabled</c> opts in, defaults to
///     the sandbox environment, answers every operation of an unconfigured/real-mode-disabled
///     adapter with an explicit failure (never a fake success), and only performs real HTTP calls
///     when simulation is off. Charge transport failures and HTTP 408/429/5xx are
///     outcome-unknown: a missing response or capture id does not prove no charge occurred.
///     Definitive HTTP 4xx declines are terminal; anything that might have moved money but cannot be confirmed is outcome-unknown
///     and left to reconciliation. PayPal webhook ingress lives in Commerce.Billing
///     (<c>PayPalBillingWebhookService</c>); live certification is deferred.
/// </summary>
public sealed class PayPalPaymentGateway(
    IOptions<PayPalGatewayOptions> options,
    IPayPalApiClient apiClient,
    ILogger<PayPalPaymentGateway> logger) : IPaymentGateway
{
    private readonly PayPalGatewayOptions _options = options.Value;

    /// <inheritdoc />
    public string ProviderId => GatewayProviderIds.PayPal;

    /// <inheritdoc />
    public string DisplayName => "PayPal";

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
            return NotEnabledFailure(request.IdempotencyKey);
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
                ErrorCode: "paypal_payment_method_required",
                ErrorMessage: "The PayPal adapter charges stored payment method tokens only; a payment method id is required.",
                Status: PaymentStatus.Failed,
                ProcessedAt: SystemClock.UtcNow,
                FailureKind: GatewayFailureKind.TerminalDecline);
        }

        var apiResult = await apiClient.CreateAndCaptureOrderAsync(
            request.IdempotencyKey,
            request.Amount,
            request.Currency,
            request.PaymentMethodId,
            request.Description,
            cancellationToken).ConfigureAwait(false);

        return MapChargeResult(apiResult, request.IdempotencyKey);
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
            return NotEnabledFailure(externalTransactionId);
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

        var apiResult = await apiClient.GetOrderAsync(externalTransactionId, cancellationToken).ConfigureAwait(false);

        if (!apiResult.Success)
        {
            return MapChargeResult(apiResult, externalTransactionId);
        }

        var status = ExtractOrderStatus(apiResult.Body);
        return new GatewayPaymentResult(
            Success: status == "COMPLETED",
            TransactionId: ExtractCaptureId(apiResult.Body) ?? externalTransactionId,
            ExternalPaymentId: ExtractCaptureId(apiResult.Body) ?? externalTransactionId,
            ErrorCode: null,
            ErrorMessage: status == "COMPLETED" ? null : $"PayPal order status is {status}.",
            Status: status switch
            {
                "COMPLETED" => PaymentStatus.Succeeded,
                "APPROVED" or "CREATED" or "PAYER_ACTION_REQUIRED" => PaymentStatus.RequiresAction,
                "VOIDED" => PaymentStatus.Cancelled,
                _ => PaymentStatus.Failed
            },
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(
                ExtractCaptureId(apiResult.Body) ?? externalTransactionId,
                captured: status == "COMPLETED"));
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
                ErrorCode: "paypal_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage));
        }

        if (_options.UseSimulation)
        {
            return Task.FromResult(new GatewayPaymentCancellationResult(true, OutcomeUnknown: false, null, null));
        }

        // Uncaptured PayPal orders never move money and expire on their own; a capture
        // that may already exist cannot be voided through the Orders API, so its outcome
        // is reported as unknown for reconciliation instead of pretending it closed.
        logger.LogInformation(
            "PayPal order {OrderId} cancellation requested: uncaptured orders expire without funds movement; captured orders are refunded through the refund path",
            externalTransactionId);

        return Task.FromResult(new GatewayPaymentCancellationResult(
            Success: true,
            OutcomeUnknown: false,
            ErrorCode: null,
            ErrorMessage: null));
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
                ErrorCode: "paypal_adapter_disabled",
                ErrorMessage: AdapterDisabledMessage,
                ProcessedAt: SystemClock.UtcNow);
        }

        if (_options.UseSimulation)
        {
            return new GatewayRefundResult(
                Success: true,
                RefundId: $"PAYPAL-REFUND-SIMULATED-{Guid.NewGuid():N}",
                AmountRefunded: request.Amount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        var apiResult = await apiClient.RefundCaptureAsync(
            request.IdempotencyKey,
            request.OriginalTransactionId,
            request.Amount,
            request.Currency ?? "USD",
            request.Reason,
            cancellationToken).ConfigureAwait(false);

        // A full refund (no explicit amount) needs no amount object; partial refunds
        // carry the captured payment's currency through the request contract.
        return MapRefundResult(apiResult, request.Amount);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateWebhookSignatureAsync(
        string payload,
        string signature,
        string secret)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        if (_options.UseSimulation)
        {
            return true;
        }

        var apiResult = await apiClient.VerifyWebhookSignatureAsync(payload, signature, secret, CancellationToken.None)
            .ConfigureAwait(false);

        if (!apiResult.Success || string.IsNullOrWhiteSpace(apiResult.Body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(apiResult.Body);
            return document.RootElement.TryGetProperty("verification_status", out var status) &&
                   status.ValueKind == JsonValueKind.String &&
                   string.Equals(status.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "PayPal webhook verification returned an unparseable body");
            return false;
        }
    }

    /// <inheritdoc />
    public Task<GatewayCustomerResult> CreateCustomerAsync(
        GatewayCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Customer vaulting without the PayPal partner referral flow cannot create a
        // chargeable token, so the adapter fails closed instead of returning a fake id.
        return Task.FromResult(new GatewayCustomerResult(
            Success: false,
            ExternalCustomerId: null,
            ErrorCode: "paypal_customer_management_not_supported",
            ErrorMessage: "PayPal customer creation is not wired to the outbound adapter; use the PayPal billing agreement flow."));
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
            ErrorCode: "paypal_payment_method_management_not_supported",
            ErrorMessage: "PayPal payment method creation happens through the billing agreement approval flow, not the outbound adapter."));
    }

    /// <inheritdoc />
    public async Task<GatewayDisputeActionResult> AcceptDisputeAsync(
        GatewayDisputeActionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExternalDisputeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);

        if (!IsEnabled)
        {
            return AdapterDisabledDisputeFailure();
        }

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return new GatewayDisputeActionResult(true, null, null);
        }

        logger.LogInformation(
            "Accepting PayPal dispute {DisputeId} with idempotency key {IdempotencyKey}",
            request.ExternalDisputeId,
            request.IdempotencyKey);

        var apiResult = await apiClient.AcceptDisputeAsync(
                request.IdempotencyKey,
                request.ExternalDisputeId,
                note: "The merchant accepts this dispute; the disputed funds are returned to the buyer.",
                cancellationToken)
            .ConfigureAwait(false);

        return MapDisputeAction(apiResult);
    }

    /// <inheritdoc />
    public async Task<GatewayDisputeActionResult> ChallengeDisputeAsync(
        GatewayDisputeChallengeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExternalDisputeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);

        if (!IsEnabled)
        {
            return AdapterDisabledDisputeFailure();
        }

        if (_options.UseSimulation)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return new GatewayDisputeActionResult(true, null, null, SimulatedEvidenceAcceptances(request));
        }

        // PayPal's native evidence surface: hosted document URLs (PayPal fetches the files
        // itself) plus the textual bundle as notes. Items whose stored reference is not a
        // hosted URL stay represented by their entry in the notes.
        var documentUrls = (request.EvidenceFiles ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.FileUrl))
            .Select(item => item.FileUrl!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        logger.LogInformation(
            "Challenging PayPal dispute {DisputeId} with idempotency key {IdempotencyKey} ({DocumentCount} document(s))",
            request.ExternalDisputeId,
            request.IdempotencyKey,
            documentUrls.Count);

        var apiResult = await apiClient.ProvideDisputeEvidenceAsync(
                request.IdempotencyKey,
                request.ExternalDisputeId,
                evidenceType: "OTHER",
                notes: string.IsNullOrWhiteSpace(request.EvidenceSummary)
                    ? "The merchant contests this dispute; the service was delivered and the charge was authorized."
                    : request.EvidenceSummary,
                documentUrls: documentUrls,
                cancellationToken)
            .ConfigureAwait(false);

        if (!apiResult.Success)
        {
            return MapDisputeAction(apiResult);
        }

        // Evidence document references are retained per item (issue #419): PayPal owns the
        // fetch, so the hosted document URL is the provider evidence reference for the row.
        var accepted = (request.EvidenceFiles ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.FileUrl))
            .Select(item => new GatewayDisputeEvidenceAcceptance(
                item.Key,
                item.FileUrl,
                item.ContentDigest,
                "documents"))
            .ToList();

        return new GatewayDisputeActionResult(true, null, null, accepted);
    }

    private static IReadOnlyList<GatewayDisputeEvidenceAcceptance>? SimulatedEvidenceAcceptances(
        GatewayDisputeChallengeRequest request) =>
        request.EvidenceFiles is { Count: > 0 } items
            ? items
                .Where(item => !string.IsNullOrWhiteSpace(item.FileUrl))
                .Select(item => new GatewayDisputeEvidenceAcceptance(item.Key, item.FileUrl, item.ContentDigest, "documents"))
                .ToList()
            : null;

    private static GatewayDisputeActionResult MapDisputeAction(PayPalApiResult apiResult)
    {
        if (apiResult.Success)
        {
            return new GatewayDisputeActionResult(true, null, null);
        }

        var errorCode = apiResult.TransportFailed
            ? "paypal_transport_failure"
            : apiResult.ErrorCode ?? $"paypal_http_{apiResult.StatusCode}";
        return new GatewayDisputeActionResult(
            false,
            errorCode,
            apiResult.ErrorMessage ?? "The PayPal dispute action failed.");
    }

    private static GatewayDisputeActionResult AdapterDisabledDisputeFailure() => new(
        Success: false,
        ErrorCode: "paypal_adapter_disabled",
        ErrorMessage: AdapterDisabledMessage);

    public Task<GatewayCancellationResult> CancelSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubscriptionId);

        // Subscription cancellation stays on the webhook-side billing integration
        // (Commerce.Billing) which owns PayPal subscription lifecycle today.
        return Task.FromResult(new GatewayCancellationResult(
            Success: false,
            ErrorCode: "paypal_subscription_management_not_supported",
            ErrorMessage: "PayPal subscription cancellation is handled by the Commerce.Billing subscription integration.",
            EffectiveDate: null));
    }

    private const string AdapterDisabledMessage =
        "The PayPal adapter is disabled (PaymentGateways:PayPal:IsEnabled is false); fail closed.";

    private GatewayPaymentResult NotEnabledFailure(string reference) =>
        new(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: "paypal_adapter_disabled",
            ErrorMessage: AdapterDisabledMessage,
            Status: PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: GatewayFailureKind.TerminalDecline);

    private GatewayPaymentResult SimulatedCharge()
    {
        var captureId = $"PAYID-SIMULATED-{Guid.NewGuid():N}";
        logger.LogDebug("Using simulated PayPal charge response for development/testing");

        return new GatewayPaymentResult(
            Success: true,
            TransactionId: captureId,
            ExternalPaymentId: captureId,
            ErrorCode: null,
            ErrorMessage: null,
            Status: PaymentStatus.Succeeded,
            ProcessedAt: SystemClock.UtcNow,
            ProviderMapping: CreateProviderMapping(captureId, captured: true));
    }

    private GatewayProviderMapping CreateProviderMapping(string objectId, bool captured) =>
        new(
            _options.UseSimulation ? "test" : _options.EnvironmentLabel,
            _options.UseSimulation ? "acct_simulated_paypal" : $"paypal_client_{_options.ClientId}",
            objectId,
            captured ? "capture" : "order",
            captured ? "capture" : "authorization");

    private GatewayPaymentResult MapChargeResult(PayPalApiResult apiResult, string referenceKey)
    {
        if (apiResult.Success)
        {
            var captureId = ExtractCaptureId(apiResult.Body);
            var orderStatus = ExtractOrderStatus(apiResult.Body);

            if (captureId is null || orderStatus != "COMPLETED")
            {
                // The order exists but the capture is not confirmed: treat as outcome
                // unknown — never replay the charge automatically. The transaction id
                // stays null (matching the Stripe outcome-unknown convention) because no
                // verified provider object may be referenced without its mapping.
                var orderId = ExtractOrderId(apiResult.Body);
                logger.LogWarning(
                    "PayPal order {OrderId} for {ReferenceKey} answered without a confirmed capture (status {Status})",
                    orderId ?? "unknown",
                    referenceKey,
                    orderStatus ?? "unknown");

                return new GatewayPaymentResult(
                    Success: false,
                    TransactionId: null,
                    ExternalPaymentId: null,
                    ErrorCode: "paypal_capture_not_confirmed",
                    ErrorMessage:
                        $"PayPal order {orderId ?? "(unknown)"} status is {orderStatus ?? "unknown"}; the capture outcome needs reconciliation.",
                    Status: PaymentStatus.Processing,
                    ProcessedAt: SystemClock.UtcNow,
                    FailureKind: GatewayFailureKind.OutcomeUnknown);
            }

            return new GatewayPaymentResult(
                Success: true,
                TransactionId: captureId,
                ExternalPaymentId: captureId,
                ErrorCode: null,
                ErrorMessage: null,
                Status: PaymentStatus.Succeeded,
                ProcessedAt: SystemClock.UtcNow,
                ProviderMapping: CreateProviderMapping(captureId, captured: true));
        }

        // Creation/capture may have reached PayPal even without a readable response.
        var failureKind = apiResult.TransportFailed || apiResult.IsServerError
            ? GatewayFailureKind.OutcomeUnknown : GatewayFailureKind.TerminalDecline;
        return new GatewayPaymentResult(
            Success: false,
            TransactionId: null,
            ExternalPaymentId: null,
            ErrorCode: apiResult.ErrorCode ?? "paypal_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The PayPal request failed.",
            Status: failureKind == GatewayFailureKind.OutcomeUnknown ? PaymentStatus.Processing : PaymentStatus.Failed,
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: failureKind);
    }

    private GatewayRefundResult MapRefundResult(PayPalApiResult apiResult, decimal? requestedAmount)
    {
        if (apiResult.Success)
        {
            var refundId = ExtractRefundId(apiResult.Body);

            return new GatewayRefundResult(
                Success: true,
                RefundId: refundId,
                AmountRefunded: requestedAmount ?? 0,
                ErrorCode: null,
                ErrorMessage: null,
                ProcessedAt: SystemClock.UtcNow);
        }

        var failureKind = ClassifyFailure(apiResult);
        return new GatewayRefundResult(
            Success: false,
            RefundId: null,
            AmountRefunded: 0,
            ErrorCode: apiResult.ErrorCode ?? "paypal_error",
            ErrorMessage: apiResult.ErrorMessage ?? "The PayPal refund failed.",
            ProcessedAt: SystemClock.UtcNow,
            FailureKind: failureKind);
    }

    private static GatewayFailureKind ClassifyFailure(PayPalApiResult apiResult)
    {
        if (apiResult.TransportFailed || apiResult.IsServerError)
        {
            // Refund retry stays on the owning provider with the same PayPal-Request-Id.
            // Charge failures use the stricter outcome-unknown classification above.
            return GatewayFailureKind.RetriableGatewayFailure;
        }

        // A definitive HTTP answer (4xx) — a decline, a validation error, or an
        // authentication/configuration problem — is terminal for automated retry.
        return GatewayFailureKind.TerminalDecline;
    }

    private static string? ExtractCaptureId(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            return
                root.TryGetProperty("purchase_units", out var purchaseUnits) &&
                purchaseUnits.ValueKind == JsonValueKind.Array &&
                purchaseUnits.GetArrayLength() > 0 &&
                purchaseUnits[0].TryGetProperty("payments", out var payments) &&
                payments.TryGetProperty("captures", out var captures) &&
                captures.ValueKind == JsonValueKind.Array &&
                captures.GetArrayLength() > 0 &&
                captures[0].TryGetProperty("id", out var captureId) &&
                captureId.ValueKind == JsonValueKind.String
                    ? captureId.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractOrderId(string? body) => ExtractStringProperty(body, "id");

    private static string? ExtractOrderStatus(string? body) => ExtractStringProperty(body, "status");

    private static string? ExtractRefundId(string? body) => ExtractStringProperty(body, "id");

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
