using System.Net;
using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Notifications.Services.Email.Renderers;

/// <summary>
/// Renders the lost-MFA-device recovery-started email (issue #1155). TRANSACTIONAL: never
/// unsubscribable, no footer. Metadata JSON: <c>{ "token": string, "email": string, "userName": string, "cooldownEndsAt": string }</c>.
/// </summary>
public sealed class MfaRecoveryStartedRenderer(
    IEmailFooterService footerService,
    IConfiguration configuration,
    NotificationMetadataProtector metadataProtector) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.MfaRecoveryStarted;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(metadataProtector.GetForRendering(notification, Type));
        if (string.IsNullOrWhiteSpace(meta.Token) || string.IsNullOrWhiteSpace(meta.Email))
        {
            throw new InvalidOperationException("Notification credential metadata is invalid or unavailable.");
        }

        var appBaseUrl = ResolveBaseUrl();
        var link = $"{appBaseUrl}/auth/mfa-recovery?token={meta.Token}";
        var cooldownEndsAt = DateTimeOffset.TryParse(meta.CooldownEndsAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : SystemClock.UtcNow;
        var cooldownText = $"{cooldownEndsAt:u} (UTC)";
        var recipientName = string.IsNullOrWhiteSpace(meta.UserName) ? meta.Email : meta.UserName;

        var plain =
            $"A request to reset the multi-factor authentication on your account was received.\n\n" +
            $"For your security, this request becomes actionable after {cooldownText}.\n\n" +
            $"After that time, open this link to continue: {link}\n\n" +
            "If you did not request this, you can ignore this email; nothing will change on your account.";
        var html =
            $"<p>A request to reset the multi-factor authentication on your account was received.</p>" +
            $"<p>For your security, this request becomes actionable after {cooldownText}.</p>" +
            $"<p>After that time, open this link to continue: <a href=\"{WebUtility.HtmlEncode(link)}\">continue MFA recovery</a></p>" +
            "<p>If you did not request this, you can ignore this email; nothing will change on your account.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(string.Empty, "Multi-factor authentication recovery requested", finalPlain, finalHtml, recipientName);
        return Task.FromResult<EmailMessage?>(message);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }

    private static MfaRecoveryStartedMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return MfaRecoveryStartedMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<MfaRecoveryStartedMetadata>(metadata, JsonOptions) ?? MfaRecoveryStartedMetadata.Empty;
        }
        catch (JsonException)
        {
            return MfaRecoveryStartedMetadata.Empty;
        }
    }

    private sealed record MfaRecoveryStartedMetadata(string? Token, string? Email, string? UserName, string? CooldownEndsAt)
    {
        public static MfaRecoveryStartedMetadata Empty { get; } = new(null, null, null, null);
    }
}

/// <summary>
/// Renders the lost-MFA-device recovery confirmation email (issue #1155). TRANSACTIONAL: never
/// unsubscribable, no footer. Metadata JSON: <c>{ "token": string, "email": string, "userName": string }</c>
/// where <c>token</c> is the one-time confirmation token.
/// </summary>
public sealed class MfaRecoveryConfirmationRenderer(
    IEmailFooterService footerService,
    IConfiguration configuration,
    NotificationMetadataProtector metadataProtector) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationType Type => NotificationType.MfaRecoveryConfirmation;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var meta = Deserialize(metadataProtector.GetForRendering(notification, Type));
        if (string.IsNullOrWhiteSpace(meta.Token) || string.IsNullOrWhiteSpace(meta.Email))
        {
            throw new InvalidOperationException("Notification credential metadata is invalid or unavailable.");
        }

        var appBaseUrl = ResolveBaseUrl();
        var link = $"{appBaseUrl}/auth/mfa-recovery?confirm={meta.Token}";
        var recipientName = string.IsNullOrWhiteSpace(meta.UserName) ? meta.Email : meta.UserName;

        var plain =
            $"Your multi-factor authentication recovery request is ready to complete.\n\n" +
            $"Open this link to confirm and disable multi-factor authentication: {link}\n\n" +
            "The link expires in one hour. If you did not request this, ignore this email.";
        var html =
            $"<p>Your multi-factor authentication recovery request is ready to complete.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Confirm MFA recovery</a></p>" +
            "<p>The link expires in one hour. If you did not request this, ignore this email.</p>";

        var (finalPlain, finalHtml) = MergeFooter(plain, html, footerService.Build(notification));
        var message = new EmailMessage(string.Empty, "Confirm multi-factor authentication recovery", finalPlain, finalHtml, recipientName);
        return Task.FromResult<EmailMessage?>(message);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }

    private static MfaRecoveryMetadata Deserialize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return MfaRecoveryMetadata.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<MfaRecoveryMetadata>(metadata, JsonOptions) ?? MfaRecoveryMetadata.Empty;
        }
        catch (JsonException)
        {
            return MfaRecoveryMetadata.Empty;
        }
    }

    private sealed record MfaRecoveryMetadata(string? Token, string? Email, string? UserName)
    {
        public static MfaRecoveryMetadata Empty { get; } = new(null, null, null);
    }
}
