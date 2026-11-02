using GameGuild.CQRS;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Command to process a Braintree webhook delivery (issue #408: Braintree signature
///     verification owned by the billing ingress). Braintree delivers form-encoded bodies
///     with the <c>bt_signature</c>/<c>bt_payload</c> pair; verification is offline.
/// </summary>
/// <param name="BtSignature">Raw <c>bt_signature</c> form field value.</param>
/// <param name="BtPayload">Raw <c>bt_payload</c> form field value (base64).</param>
public sealed record ProcessBraintreeWebhookCommand(
    string BtSignature,
    string BtPayload) : ICommand<WebhookProcessingResult>;
