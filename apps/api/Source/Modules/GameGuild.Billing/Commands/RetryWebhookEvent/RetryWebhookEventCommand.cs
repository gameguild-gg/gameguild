using GameGuild.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Billing.Commands;

/// <summary>
///     Command to retry a failed webhook event
/// </summary>
public record RetryWebhookEventCommand(string EventId) : ICommand<WebhookRetryResult>;
