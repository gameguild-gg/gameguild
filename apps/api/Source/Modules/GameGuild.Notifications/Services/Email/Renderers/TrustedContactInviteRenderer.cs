using System.Net;
using System.Text.Json;
using GameGuild.Email;

namespace GameGuild.Notifications.Services.Email.Renderers;

/// <summary>
/// Renders trusted-recovery-contact invitation emails (issue #1145) from the notification row's
/// metadata. Transactional type: never footer-eligible, so the footer service returns null and
/// the body is unchanged. The row is queued by the Identity.Authentication module's
/// AddTrustedContact command through the durable notification outbox; the dispatcher renders
/// and (re)sends it independently of the originating command.
/// </summary>
public sealed class TrustedContactInviteRenderer(IEmailFooterService footerService) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.TrustedContactInvite;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(notification.Metadata);
        var ownerEmail = string.IsNullOrWhiteSpace(meta.OwnerEmail) ? "a GameGuild account" : meta.OwnerEmail.Trim();
        var confirmUrl = meta.ConfirmUrl ?? string.Empty;
        var expiresAt = meta.ExpiresAt;

        var subject = "Trusted recovery contact invitation";
        var plain =
            $"You were designated as a trusted recovery contact for a GameGuild account ({ownerEmail}).\n\n" +
            $"If you accept, open this link before {expiresAt} (UTC) to confirm: {confirmUrl}\n\n" +
            "If you were not expecting this, ignore this email; confirming is voluntary and you can ask the account owner to remove you at any time.";
        var html =
            $"<p>You were designated as a trusted recovery contact for a GameGuild account ({WebUtility.HtmlEncode(ownerEmail)}).</p>" +
            $"<p>If you accept, open this link before {WebUtility.HtmlEncode(expiresAt)} (UTC) to confirm: <a href=\"{WebUtility.HtmlEncode(confirmUrl)}\">confirm trusted contact</a></p>" +
            "<p>If you were not expecting this, ignore this email; confirming is voluntary and you can ask the account owner to remove you at any time.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(
            notification.RecipientEmail ?? string.Empty,
            subject,
            finalPlain,
            finalHtml);
        return Task.FromResult<EmailMessage?>(message);
    }

    private static TrustedContactInviteMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return TrustedContactInviteMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<TrustedContactInviteMetadata>(metadata, JsonOptions) ?? TrustedContactInviteMetadata.Empty;
        }
        catch (JsonException)
        {
            return TrustedContactInviteMetadata.Empty;
        }
    }

    private sealed record TrustedContactInviteMetadata(
        string? InviteToken,
        string? OwnerEmail,
        string? ConfirmUrl,
        string? ExpiresAt)
    {
        public static TrustedContactInviteMetadata Empty { get; } = new(null, null, null, null);
    }
}
