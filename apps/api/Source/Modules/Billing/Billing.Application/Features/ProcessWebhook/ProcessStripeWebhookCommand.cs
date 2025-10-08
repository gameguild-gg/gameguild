using GameGuild.Modules.Billing.Models;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Command to process a Stripe webhook
/// </summary>
public record ProcessStripeWebhookCommand(
    string Payload,
    string Signature
) : ICommand<WebhookProcessingResult>;

