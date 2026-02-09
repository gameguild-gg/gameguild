using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process an Apple Pay webhook.
///     Includes Apple Pay-specific headers required for verification.
/// </summary>
/// <param name="Payload">Raw JSON payload from Apple Pay</param>
/// <param name="MerchantId">Apple Pay merchant identifier</param>
/// <param name="Signature">Apple Pay signature for verification</param>
public sealed record ProcessApplePayWebhookCommand(
    string Payload,
    string MerchantId,
    string Signature) : ICommand<WebhookProcessingResult>;
