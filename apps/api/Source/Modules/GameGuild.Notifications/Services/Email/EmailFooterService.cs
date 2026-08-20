using System.Net;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Footer content appended to suppressible email bodies: a one-click unsubscribe link scoped to the
/// email's type plus a "manage all notifications" link. Transactional types and null-recipient rows
/// (e.g. tenant invites to unregistered addresses) NEVER get a footer — no token is ever generated for them.
/// </summary>
public sealed record EmailFooter(string PlainText, string Html);

/// <summary>
/// Builds the plain + html footer for suppressible emails. A clean leaf service: renderers receive it via
/// constructor injection (T6-T9) and merge the footer into their body through <see cref="EmailRendererBase"/>.
/// </summary>
public interface IEmailFooterService
{
    /// <summary>
    /// Builds the footer for the given notification, or null when the email is not footer-eligible
    /// (transactional type, or RecipientId is null — the null-recipient invariant: never footer, no token).
    /// </summary>
    EmailFooter? Build(Notification notification);
}

/// <inheritdoc />
public sealed class EmailFooterService(
    IUnsubscribeTokenService unsubscribeTokenService,
    IConfiguration configuration) : IEmailFooterService
{
    private const string ManagePath = "/workspace/settings/notifications";
    private const string UnsubscribePath = "/unsubscribe";

    public EmailFooter? Build(Notification notification)
    {
        if (notification.RecipientId is null || NotificationCategories.Transactional.Contains(notification.Type))
        {
            return null;
        }

        var baseUrl = ResolveBaseUrl();
        var typeName = notification.Type.ToString();
        var token = unsubscribeTokenService.Generate(notification.RecipientId.Value, "type", typeName);
        var unsubscribeUrl = $"{baseUrl}{UnsubscribePath}?token={Uri.EscapeDataString(token)}";
        var manageUrl = $"{baseUrl}{ManagePath}";

        var plainText =
            $"You are receiving this because you have a GameGuild account.\n" +
            $"To stop receiving this type of email, unsubscribe here:\n{unsubscribeUrl}\n" +
            $"To manage all your notification preferences, visit:\n{manageUrl}";

        var html =
            $"<p style=\"margin-top:24px;padding-top:16px;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;\">" +
            $"You are receiving this because you have a GameGuild account. " +
            $"<a href=\"{WebUtility.HtmlEncode(unsubscribeUrl)}\">Unsubscribe from this type of email</a> · " +
            $"<a href=\"{WebUtility.HtmlEncode(manageUrl)}\">Manage all notifications</a>.</p>";

        return new EmailFooter(plainText, html);
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }
}
