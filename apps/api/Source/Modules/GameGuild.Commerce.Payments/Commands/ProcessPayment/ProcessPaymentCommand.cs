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
/// <param name="CardBin">
///     Optional 6-8 digit Bank Identification Number prefix of the presented card; feeds
///     the high-risk BIN range fraud signal (issue #409). PCI-permitted non-sensitive prefix.
/// </param>
/// <param name="AvsOutcome">
///     Optional server-known Address Verification System result (issue #409). Reserved for
///     server-side callers (gateway-verified flows, retry paths after gateway declines) —
///     never populated from client input.
/// </param>
/// <param name="CvvOutcome">
///     Optional server-known card security code result (issue #409). Same provenance rules
///     as <paramref name="AvsOutcome"/>.
/// </param>
/// <param name="ClientTimezone">
///     Optional IANA timezone the client claims; compared against the GeoIP-resolved
///     timezone of <paramref name="SourceIpAddress"/> (issue #409).
/// </param>
public sealed record ProcessPaymentCommand(
    Guid TenantId,
    Guid SubscriptionId,
    decimal Amount,
    string PaymentMethodId,
    string? Country = null,
    string? GatewayProvider = null,
    string? SourceIpAddress = null,
    string? DeviceFingerprint = null,
    string? PayerEmail = null,
    string? CardBin = null,
    FraudVerificationOutcome? AvsOutcome = null,
    FraudVerificationOutcome? CvvOutcome = null,
    string? ClientTimezone = null) : ICommand<PaymentResult>;
