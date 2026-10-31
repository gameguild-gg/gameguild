using GameGuild.CQRS;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Port for the WhatsApp Business Cloud API (issues #54/#55). The notification pipeline
///     and the broadcast engine depend on this abstraction only; the composition root plugs
///     in <see cref="WhatsAppBusinessApiProvider"/> when credentials exist, or serves the
///     fail-closed <see cref="NoOpWhatsAppProvider"/> otherwise (same pattern as the Plaid
///     instant-verification seam). Credentials are resolved exclusively from environment
///     variables — never from configuration files or source code.
/// </summary>
public interface IWhatsAppProvider
{
    /// <summary>
    ///     Whether a real WhatsApp Business adapter is configured. Callers use this to
    ///     decide between offering WhatsApp delivery and failing closed.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    ///     Sends a free-form text message to a WhatsApp user. Outside an approved
    ///     24-hour customer service window the Cloud API only accepts templates; the
    ///     provider surfaces that as a failure rather than silently dropping the message.
    /// </summary>
    /// <param name="message">Outbound message (E.164 destination and body).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider message id on acceptance, or a failure.</returns>
    Task<Result<WhatsAppSendResult>> SendMessageAsync(WhatsAppOutboundMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends a pre-approved message template (required for business-initiated
    ///     conversations). The template name must pass the configured allowlist.
    /// </summary>
    /// <param name="message">Template message with placeholder variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider message id on acceptance, or a failure.</returns>
    Task<Result<WhatsAppSendResult>> SendTemplateAsync(WhatsAppTemplateMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies a phone number is reachable on WhatsApp before it is used as a
    ///     notification destination.
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification outcome with the canonical number when valid.</returns>
    Task<Result<WhatsAppNumberVerification>> VerifyNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the current delivery status of a previously accepted message.
    /// </summary>
    /// <param name="providerMessageId">Message id returned by a successful send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider-side delivery status, or a failure.</returns>
    Task<Result<WhatsAppMessageStatus>> GetMessageStatusAsync(string providerMessageId, CancellationToken cancellationToken = default);
}

/// <summary>An outbound free-form WhatsApp text message.</summary>
/// <param name="To">Destination phone number in E.164 format (digits only, no plus sign for the Cloud API).</param>
/// <param name="Body">Plain-text message body.</param>
public sealed record WhatsAppOutboundMessage(string To, string Body);

/// <summary>An outbound WhatsApp template message.</summary>
/// <param name="To">Destination phone number in E.164 format.</param>
/// <param name="TemplateName">Name of a pre-approved WhatsApp message template.</param>
/// <param name="LanguageCode">Template language (defaults to <c>en_US</c> when null/empty).</param>
/// <param name="Placeholders">Named template variables mapped to their values.</param>
public sealed record WhatsAppTemplateMessage(
    string To,
    string TemplateName,
    string? LanguageCode,
    IReadOnlyDictionary<string, string>? Placeholders);

/// <summary>Acceptance receipt for an outbound WhatsApp message.</summary>
/// <param name="ProviderMessageId">WhatsApp-assigned message id (WAMID) for delivery tracking.</param>
public sealed record WhatsAppSendResult(string ProviderMessageId);

/// <summary>Outcome of a WhatsApp number verification.</summary>
/// <param name="IsValid">Whether the number is reachable on WhatsApp.</param>
/// <param name="CanonicalPhoneNumber">The provider-normalized E.164 number when valid.</param>
public sealed record WhatsAppNumberVerification(bool IsValid, string? CanonicalPhoneNumber);

/// <summary>Provider-side delivery status of an outbound WhatsApp message.</summary>
/// <param name="Status">Raw provider status string (e.g. "sent", "delivered", "read", "failed").</param>
/// <param name="ErrorCode">Provider error code when the status is "failed".</param>
/// <param name="ErrorMessage">Provider error title when available.</param>
public sealed record WhatsAppMessageStatus(string Status, string? ErrorCode, string? ErrorMessage);
