using System.Net;
using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications.Services.Email;

namespace GameGuild.Notifications.Services.Email.Renderers;

/// <summary>
/// Renders tenant-invite emails (new invite and resend) from the row's metadata. Transactional type:
/// never footer-eligible, so the footer service returns null and the body is unchanged.
/// </summary>
public sealed class TenantInviteRenderer(IEmailFooterService footerService) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.TenantInvite;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(notification.Metadata);
        var recipientName = string.IsNullOrWhiteSpace(meta.InviteeName)
            ? notification.RecipientEmail?.Trim() ?? string.Empty
            : meta.InviteeName.Trim();
        var inviter = string.IsNullOrWhiteSpace(meta.InvitedByEmail)
            ? "A GameGuild administrator"
            : meta.InvitedByEmail.Trim();
        var tenantName = string.IsNullOrWhiteSpace(meta.TenantName) ? "GameGuild" : meta.TenantName;
        var role = meta.Role ?? string.Empty;
        var reviewUrl = meta.ReviewUrl ?? string.Empty;
        var activationUrl = meta.ActivationUrl ?? string.Empty;

        string subject, plain, html;
        if (meta.Resend)
        {
            subject = $"Reminder: you were invited to {tenantName} on GameGuild";
            plain =
                $"Hi {recipientName},\n\n{inviter} resent your invitation to join {tenantName} on GameGuild as {role}.\n\nReview and accept your access:\n{reviewUrl}\n\nIf this is your first GameGuild invitation, set your password first:\n{activationUrl}\n\nIf you were not expecting this invite, you can ignore this email.";
            html =
                $"<p>Hi {WebUtility.HtmlEncode(recipientName)},</p><p>{WebUtility.HtmlEncode(inviter)} resent your invitation to join <strong>{WebUtility.HtmlEncode(tenantName)}</strong> on GameGuild as <strong>{WebUtility.HtmlEncode(role)}</strong>.</p><p><a href=\"{WebUtility.HtmlEncode(reviewUrl)}\">Review and accept your access</a></p><p>First time on GameGuild? <a href=\"{WebUtility.HtmlEncode(activationUrl)}\">Set your password</a>, then return to your invitations.</p><p>If you were not expecting this invite, you can ignore this email.</p>";
        }
        else
        {
            subject = $"You were invited to {tenantName} on GameGuild";
            plain =
                $"Hi {recipientName},\n\n{inviter} invited you to join {tenantName} on GameGuild as {role}.\n\nReview and accept your access:\n{reviewUrl}\n\nIf this is your first GameGuild invitation, set your password first:\n{activationUrl}\n\nIf you were not expecting this invite, you can ignore this email.";
            html =
                $"<p>Hi {WebUtility.HtmlEncode(recipientName)},</p><p>{WebUtility.HtmlEncode(inviter)} invited you to join <strong>{WebUtility.HtmlEncode(tenantName)}</strong> on GameGuild as <strong>{WebUtility.HtmlEncode(role)}</strong>.</p><p><a href=\"{WebUtility.HtmlEncode(reviewUrl)}\">Review and accept your access</a></p><p>First time on GameGuild? <a href=\"{WebUtility.HtmlEncode(activationUrl)}\">Set your password</a>, then return to your invitations.</p><p>If you were not expecting this invite, you can ignore this email.</p>";
        }

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(
            notification.RecipientEmail ?? string.Empty,
            subject,
            finalPlain,
            finalHtml,
            recipientName);
        return Task.FromResult<EmailMessage?>(message);
    }

    private static TenantInviteMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return TenantInviteMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<TenantInviteMetadata>(metadata, JsonOptions) ?? TenantInviteMetadata.Empty;
        }
        catch (JsonException)
        {
            return TenantInviteMetadata.Empty;
        }
    }

    private sealed record TenantInviteMetadata(
        string? InviteeName,
        string? InvitedByEmail,
        string? TenantName,
        string? Role,
        string? ReviewUrl,
        string? ActivationUrl,
        bool Resend)
    {
        public static TenantInviteMetadata Empty { get; } = new(null, null, null, null, null, null, false);
    }
}
