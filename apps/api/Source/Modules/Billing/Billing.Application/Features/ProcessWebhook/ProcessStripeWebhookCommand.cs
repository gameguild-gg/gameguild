using GameGuild.Modules.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Command to process a Stripe webhook
/// </summary>
public record ProcessStripeWebhookCommand(
    string Payload,
    string Signature
) : ICommand<WebhookProcessingResult>;

