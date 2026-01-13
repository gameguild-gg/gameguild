using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a PayPal webhook
/// </summary>
public record ProcessPayPalWebhookCommand(string Payload) : ICommand<WebhookProcessingResult>;
