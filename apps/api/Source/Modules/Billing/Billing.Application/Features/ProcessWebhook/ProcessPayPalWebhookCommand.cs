using GameGuild.Modules.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Command to process a PayPal webhook
/// </summary>
public record ProcessPayPalWebhookCommand(
    string Payload
) : ICommand<WebhookProcessingResult>;

