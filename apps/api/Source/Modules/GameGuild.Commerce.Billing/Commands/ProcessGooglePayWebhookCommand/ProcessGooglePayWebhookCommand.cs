using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a Google Pay webhook
/// </summary>
/// <param name="Payload">The webhook payload from Google Pay</param>
/// <param name="AuthHeader">The authorization header containing JWT token</param>
/// <param name="ProjectId">The Google Cloud project ID for validation</param>
public sealed record ProcessGooglePayWebhookCommand(string Payload, string AuthHeader, string ProjectId) : ICommand<WebhookProcessingResult>;
