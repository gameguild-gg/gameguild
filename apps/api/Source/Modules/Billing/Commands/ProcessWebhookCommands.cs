using GameGuild.Modules.Billing.Models;
using MediatR;

namespace GameGuild.Modules.Billing.Commands;

/// <summary>
/// Command to process a generic billing webhook
/// </summary>
public record ProcessBillingWebhookCommand(
    string Provider,
    string Payload,
    Dictionary<string, string> Headers
) : IRequest<WebhookProcessingResult>;

/// <summary>
/// Command to process a Stripe webhook
/// </summary>
public record ProcessStripeWebhookCommand(
    string Payload,
    string SignatureHeader
) : IRequest<WebhookProcessingResult>;

/// <summary>
/// Command to process a PayPal webhook
/// </summary>
public record ProcessPayPalWebhookCommand(
    string Payload,
    Dictionary<string, string> Headers
) : IRequest<WebhookProcessingResult>;
