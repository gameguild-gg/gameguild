using System.Net;
using System.Text;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Renders one bundled digest email from claimed HeldForDigest rows: items grouped by notification type,
/// one line per item (title, UTC time, action URL). The digest is suppressible as a whole, so the footer
/// carries a manage-preferences link only — no per-type unsubscribe tokens.
/// </summary>
public sealed class DigestRenderer(IConfiguration configuration)
{
    public const string Subject = "Your GameGuild digest";

    private const string ManagePath = "/workspace/settings/notifications";

    /// <summary>Renders the digest email for a batch of claimed rows.</summary>
    public EmailMessage Render(string toEmail, IReadOnlyList<Notification> rows)
    {
        var baseUrl = ResolveBaseUrl();
        var manageUrl = $"{baseUrl}{ManagePath}";

        var plain = new StringBuilder();
        var html = new StringBuilder();

        plain.AppendLine("Here is what you missed on GameGuild:");
        html.Append("<p>Here is what you missed on GameGuild:</p>");

        foreach (var group in rows.GroupBy(n => n.Type).OrderBy(g => g.Key.ToString(), StringComparer.Ordinal))
        {
            var typeLabel = group.Key.ToString();

            plain.AppendLine().AppendLine(typeLabel + ":");
            html.Append($"<h3>{WebUtility.HtmlEncode(typeLabel)}</h3><ul>");

            foreach (var row in group.OrderBy(n => n.CreatedAt))
            {
                var time = $"{row.CreatedAt:yyyy-MM-dd HH:mm} UTC";
                plain.AppendLine($"- {row.Title} ({time}){(string.IsNullOrWhiteSpace(row.ActionUrl) ? "" : $" — {row.ActionUrl}")}");

                html.Append("<li>");
                if (string.IsNullOrWhiteSpace(row.ActionUrl))
                {
                    html.Append(WebUtility.HtmlEncode(row.Title));
                }
                else
                {
                    html.Append($"<a href=\"{WebUtility.HtmlEncode(row.ActionUrl)}\">{WebUtility.HtmlEncode(row.Title)}</a>");
                }
                html.Append($" <span style=\"color:#6b7280;font-size:12px;\">({WebUtility.HtmlEncode(time)})</span></li>");
            }

            html.Append("</ul>");
        }

        plain.AppendLine()
            .AppendLine("---")
            .AppendLine($"Manage your notification preferences:\n{manageUrl}");

        html.Append($"<p style=\"margin-top:24px;padding-top:16px;border-top:1px solid #e5e7eb;color:#6b7280;font-size:12px;\">")
            .Append($"Manage your <a href=\"{WebUtility.HtmlEncode(manageUrl)}\">notification preferences</a>.</p>");

        return new EmailMessage(toEmail, Subject, plain.ToString(), html.ToString());
    }

    private string ResolveBaseUrl()
    {
        var appBaseUrl = configuration["App:BaseUrl"];
        return string.IsNullOrWhiteSpace(appBaseUrl) ? "http://localhost:3000" : appBaseUrl.TrimEnd('/');
    }
}
