using GameGuild.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Billing.Commands;

/// <summary>
///     Command to process a PayPal webhook
/// </summary>
public record ProcessPayPalWebhookCommand(string Payload) : ICommand<WebhookProcessingResult>;
