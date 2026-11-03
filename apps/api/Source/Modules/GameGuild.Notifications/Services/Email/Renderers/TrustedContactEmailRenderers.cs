using System.Net;
using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Notifications.Services.Email.Renderers;

/// <summary>
/// Renders the trusted-contact invitation email (issue #1145). TRANSACTIONAL: never
/// unsubscribable, no footer. Metadata JSON:
/// <c>{ "token": string, "contactEmail": string, "ownerEmail": string, "expiresAt": string }</c>.
/// </summary>
public sealed class TrustedContactInviteRenderer(
    IEmailFooterService footerService,
    IConfiguration configuration,
    NotificationMetadataProtector metadataProtector) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.TrustedContactInvite;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(metadataProtector.GetForRendering(notification, Type));
        if (string.IsNullOrWhiteSpace(meta.Token) || string.IsNullOrWhiteSpace(meta.ContactEmail) ||
            string.IsNullOrWhiteSpace(meta.OwnerEmail))
        {
            throw new InvalidOperationException("Notification credential metadata is invalid or unavailable.");
        }
        var appBaseUrl = ResolveBaseUrl();
        var link = $"{appBaseUrl}/auth/trusted-contacts/confirm?token={Uri.EscapeDataString(meta.Token)}";
        var expiresAt = DateTimeOffset.TryParse(meta.ExpiresAt, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) ? parsed : (DateTimeOffset?)null;
        var expiryText = expiresAt is { } endsAt ? $"{endsAt:u} (UTC)" : "its stated expiry";
        var encodedLink = WebUtility.HtmlEncode(link);

        var plain =
            $"You were designated as a trusted recovery contact for a GameGuild account ({meta.OwnerEmail}).\n\n" +
            $"If you accept, open this link before {expiryText} to confirm: {link}\n\n" +
            "If you were not expecting this, ignore this email; confirming is voluntary and you can ask the account owner to remove you at any time.";
        var html =
            $"<p>You were designated as a trusted recovery contact for a GameGuild account ({WebUtility.HtmlEncode(meta.OwnerEmail)}).</p>" +
            $"<p>If you accept, open this link before {WebUtility.HtmlEncode(expiryText)} to confirm: <a href=\"{encodedLink}\">confirm trusted contact</a></p>" +
            "<p>If you were not expecting this, ignore this email; confirming is voluntary and you can ask the account owner to remove you at any time.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(string.Empty, "Trusted recovery contact invitation", finalPlain, finalHtml, meta.ContactEmail);
        return Task.FromResult<EmailMessage?>(message);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }

    private static InviteMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return InviteMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<InviteMetadata>(metadata, JsonOptions) ?? InviteMetadata.Empty;
        }
        catch (JsonException)
        {
            return InviteMetadata.Empty;
        }
    }

    private sealed record InviteMetadata(string? Token, string? ContactEmail, string? OwnerEmail, string? ExpiresAt)
    {
        public static InviteMetadata Empty { get; } = new(null, null, null, null);
    }
}

/// <summary>
/// Renders the trusted-contact recovery approval-code email (issue #1145). TRANSACTIONAL: never
/// unsubscribable, no footer. Metadata JSON:
/// <c>{ "code": string, "contactEmail": string, "ownerEmail": string, "windowStart": string, "windowEnd": string }</c>.
/// </summary>
public sealed class TrustedContactRecoveryCodeRenderer(
    IEmailFooterService footerService,
    IConfiguration configuration,
    NotificationMetadataProtector metadataProtector) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.TrustedContactRecoveryCode;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(metadataProtector.GetForRendering(notification, Type));
        if (string.IsNullOrWhiteSpace(meta.Code) || string.IsNullOrWhiteSpace(meta.ContactEmail) ||
            string.IsNullOrWhiteSpace(meta.OwnerEmail))
        {
            throw new InvalidOperationException("Notification credential metadata is invalid or unavailable.");
        }

        var appBaseUrl = ResolveBaseUrl();
        var link = $"{appBaseUrl}/auth/account-recovery/approve";
        var encodedLink = WebUtility.HtmlEncode(link);
        var plain =
            $"A recovery was requested for the GameGuild account {meta.OwnerEmail}, which lists you as a trusted recovery contact.\n\n" +
            $"Approvals are accepted from {meta.WindowStart} (UTC) until {meta.WindowEnd} (UTC).\n" +
            $"Your single-use recovery code: {meta.Code}\n\n" +
            $"Submit it at {link} if you verified with the account owner that this request is legitimate.\n" +
            "If you were not expecting this, ignore this email; codes expire with the recovery request.";
        var html =
            $"<p>A recovery was requested for the GameGuild account {WebUtility.HtmlEncode(meta.OwnerEmail)}, which lists you as a trusted recovery contact.</p>" +
            $"<p>Approvals are accepted from {WebUtility.HtmlEncode(meta.WindowStart ?? string.Empty)} (UTC) until {WebUtility.HtmlEncode(meta.WindowEnd ?? string.Empty)} (UTC).</p>" +
            $"<p>Your single-use recovery code: <code>{WebUtility.HtmlEncode(meta.Code)}</code></p>" +
            $"<p>Submit it at <a href=\"{encodedLink}\">{encodedLink}</a> if you verified with the account owner that this request is legitimate.</p>" +
            "<p>If you were not expecting this, ignore this email; codes expire with the recovery request.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(string.Empty, "Account recovery approval code", finalPlain, finalHtml, meta.ContactEmail);
        return Task.FromResult<EmailMessage?>(message);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }

    private static CodeMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return CodeMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<CodeMetadata>(metadata, JsonOptions) ?? CodeMetadata.Empty;
        }
        catch (JsonException)
        {
            return CodeMetadata.Empty;
        }
    }

    private sealed record CodeMetadata(string? Code, string? ContactEmail, string? OwnerEmail, string? WindowStart, string? WindowEnd)
    {
        public static CodeMetadata Empty { get; } = new(null, null, null, null, null);
    }
}

/// <summary>
/// Renders the account-owner notice that a delegated recovery was requested (issue #1145).
/// TRANSACTIONAL: never unsubscribable, no footer. Carries no bearer material in metadata:
/// <c>{ "ownerEmail": string, "windowStart": string, "windowEnd": string }</c>.
/// </summary>
public sealed class AccountRecoveryOwnerNoticeRenderer(
    IEmailFooterService footerService,
    IConfiguration configuration) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.AccountRecoveryOwnerNotice;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // No bearer material in this metadata, so it is not credential-protected (see
        // NotificationMetadataProtector.RequiresProtection) and is read as stored.
        var meta = Deserialize(notification.Metadata);
        if (string.IsNullOrWhiteSpace(meta.OwnerEmail))
        {
            throw new InvalidOperationException("Notification metadata is invalid or unavailable.");
        }
        var appBaseUrl = ResolveBaseUrl();
        var link = $"{appBaseUrl}/auth/account-recovery/cancel";
        var encodedLink = WebUtility.HtmlEncode(link);

        var plain = "A delegated account recovery was requested for your account.\n\n" +
            $"Your trusted contacts were emailed single-use approval codes. Approvals are accepted from {meta.WindowStart} (UTC) until {meta.WindowEnd} (UTC).\n\n" +
            $"If this was not you, cancel the recovery at {link} while you still have access, or contact support immediately.";
        var html = "<p>A delegated account recovery was requested for your account.</p>" +
            $"<p>Your trusted contacts were emailed single-use approval codes. Approvals are accepted from {WebUtility.HtmlEncode(meta.WindowStart ?? string.Empty)} (UTC) until {WebUtility.HtmlEncode(meta.WindowEnd ?? string.Empty)} (UTC).</p>" +
            $"<p>If this was not you, <a href=\"{encodedLink}\">cancel the recovery</a> while you still have access, or contact support immediately.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(string.Empty, "Account recovery requested", finalPlain, finalHtml, meta.OwnerEmail);
        return Task.FromResult<EmailMessage?>(message);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }

    private static NoticeMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return NoticeMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<NoticeMetadata>(metadata, JsonOptions) ?? NoticeMetadata.Empty;
        }
        catch (JsonException)
        {
            return NoticeMetadata.Empty;
        }
    }

    private sealed record NoticeMetadata(string? OwnerEmail, string? WindowStart, string? WindowEnd)
    {
        public static NoticeMetadata Empty { get; } = new(null, null, null);
    }
}
