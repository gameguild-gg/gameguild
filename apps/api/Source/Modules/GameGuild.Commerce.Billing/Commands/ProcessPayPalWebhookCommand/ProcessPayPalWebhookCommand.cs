using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a PayPal webhook.
///     Includes PayPal-specific headers required for signature verification.
/// </summary>
/// <param name="Payload">Raw JSON payload from PayPal</param>
/// <param name="TransmissionId">PayPal transmission ID (PAYPAL-TRANSMISSION-ID header)</param>
/// <param name="TransmissionSignature">PayPal signature (PAYPAL-TRANSMISSION-SIG header)</param>
/// <param name="TransmissionTime">PayPal transmission time (PAYPAL-TRANSMISSION-TIME header)</param>
/// <param name="CertUrl">PayPal certificate URL (PAYPAL-CERT-URL header)</param>
/// <param name="AuthAlgo">PayPal auth algorithm (PAYPAL-AUTH-ALGO header)</param>
/// <param name="WebhookId">PayPal webhook ID from configuration</param>
public record ProcessPayPalWebhookCommand(
    string Payload,
    string TransmissionId,
    string TransmissionSignature,
    string TransmissionTime,
    string? CertUrl = null,
    string? AuthAlgo = null,
    string? WebhookId = null) : ICommand<WebhookProcessingResult>;
