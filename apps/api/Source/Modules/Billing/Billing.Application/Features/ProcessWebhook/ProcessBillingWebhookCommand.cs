using GameGuild.Modules.Billing.Models;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ProcessWebhook;

/// <summary>
///     Command to process a generic billing webhook
/// </summary>
public record ProcessBillingWebhookCommand(
    string Provider,
    string Payload,
    Dictionary<string, string> Headers
) : ICommand<WebhookProcessingResult>;

