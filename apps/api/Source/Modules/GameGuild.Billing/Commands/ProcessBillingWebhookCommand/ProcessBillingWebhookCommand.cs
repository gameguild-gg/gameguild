using GameGuild.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Billing.Commands;

/// <summary>
///     Command to process a generic billing webhook
/// </summary>
public record ProcessBillingWebhookCommand(string Provider, string Payload, Dictionary<string, string> Headers) : ICommand<WebhookProcessingResult>;
