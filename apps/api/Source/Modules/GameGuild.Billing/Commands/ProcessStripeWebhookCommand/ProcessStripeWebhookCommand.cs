using GameGuild.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Billing.Commands;

/// <summary>
///     Command to process a Stripe webhook
/// </summary>
public record ProcessStripeWebhookCommand(string Payload, string Signature) : ICommand<WebhookProcessingResult>;
