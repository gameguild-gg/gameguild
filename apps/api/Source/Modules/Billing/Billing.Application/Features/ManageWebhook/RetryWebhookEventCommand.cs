using GameGuild.Modules.Billing.Models;
using MediatR;

namespace GameGuild.Modules.Billing.Features.ManageWebhook;

/// <summary>
///     Command to retry a failed webhook event
/// </summary>
public record RetryWebhookEventCommand(
    string EventId
) : ICommand<WebhookRetryResult>;

