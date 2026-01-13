using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a Stripe webhook
/// </summary>
public record ProcessStripeWebhookCommand(string Payload, string Signature) : ICommand<WebhookProcessingResult>;
