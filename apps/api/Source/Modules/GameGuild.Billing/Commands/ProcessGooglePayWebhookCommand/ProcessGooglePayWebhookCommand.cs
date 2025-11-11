using GameGuild.Billing.Models;
using GameGuild.CQRS;

namespace GameGuild.Billing.Commands;

/// <summary>
///     Command to process a Google Pay webhook
/// </summary>
/// <param name="Payload">The webhook payload from Google Pay</param>
/// <param name="AuthHeader">The authorization header containing JWT token</param>
/// <param name="ProjectId">The Google Cloud project ID for validation</param>
public record ProcessGooglePayWebhookCommand(string Payload, string AuthHeader, string ProjectId) : ICommand<WebhookProcessingResult>;
