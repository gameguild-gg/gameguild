using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a generic billing webhook
/// </summary>
public sealed record ProcessBillingWebhookCommand(string Provider, string Payload, Dictionary<string, string> Headers) : ICommand<WebhookProcessingResult>;
