using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to process a payment
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="SubscriptionId">Subscription ID</param>
/// <param name="Amount">Payment amount</param>
/// <param name="PaymentMethodId">Payment method identifier</param>
/// <param name="Country">
///     Optional ISO 3166-1 alpha-2 billing country (issue #413): feeds the geography-aware
///     gateway routing filters/strategy, the geographic analytics breakdown and the
///     high-risk-country fraud signal (issue #409).
/// </param>
/// <param name="GatewayProvider">Explicit provider for opaque gateway tokens; known prefixes may omit it.</param>
/// <param name="SourceIpAddress">Optional client IP of the attempt; feeds the IP blacklist fraud signal (issue #409).</param>
/// <param name="DeviceFingerprint">Optional device fingerprint of the attempt; feeds the device blacklist fraud signal (issue #409).</param>
/// <param name="PayerEmail">Optional payer email; feeds the disposable-email and email blacklist fraud signals (issue #409).</param>
public sealed record ProcessPaymentCommand(
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    string PaymentMethodId,
    string? Country = null,
    string? GatewayProvider = null,
    string? SourceIpAddress = null,
    string? DeviceFingerprint = null,
    string? PayerEmail = null) : ICommand<PaymentResult>;
