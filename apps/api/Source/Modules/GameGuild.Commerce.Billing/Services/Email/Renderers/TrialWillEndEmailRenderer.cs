using System.Globalization;
using System.Text.Json;
using GameGuild.Email;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Commerce.Billing.Services.Email.Renderers;

/// <summary>
///     Renders the trial-ending reminder email (issue #408: "Trial ending → Send reminder email")
///     at send time from <see cref="Notification.Metadata"/>, written by
///     <c>StripeTrialWillEndConsumer</c> when a verified <c>customer.subscription.trial_will_end</c>
///     Stripe webhook arrives. Registered as <see cref="IEmailRenderer"/> for
///     <see cref="NotificationType.Billing"/> in the Billing module DI.
/// </summary>
public sealed class TrialWillEndEmailRenderer(
    IConfiguration configuration,
    IEmailFooterService footerService) : EmailRendererBase, IEmailRenderer
{
    private static readonly JsonSerializerOptions MetadataOptions = new(JsonSerializerDefaults.Web);
    private static readonly CultureInfo AmountCulture = CultureInfo.GetCultureInfo("en-US");

    public NotificationType Type => NotificationType.Billing;

    public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var metadata = ParseMetadata(notification.Metadata);
        var recipientName = string.IsNullOrWhiteSpace(metadata.RecipientName)
            ? notification.RecipientEmail?.Trim() ?? string.Empty
            : metadata.RecipientName.Trim();

        var trialEndLabel = metadata.TrialEndUtc.HasValue
            ? metadata.TrialEndUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "soon";
        var amountLabel = $"{metadata.Amount.ToString("F2", AmountCulture)} {metadata.Currency}";

        var consoleBaseUrl = ResolveConsoleBaseUrl();
        var subscriptionsUrl = BuildAbsoluteUrl(consoleBaseUrl, $"subscriptions/{metadata.SubscriptionId}");

        var subject = "Your GameGuild trial is ending soon";

        var plainTextBody =
            $"Hi {recipientName},\n\n" +
            $"Your subscription trial ends on {trialEndLabel}. " +
            $"When it ends, your plan ({amountLabel}) starts automatically and your access continues without interruption.\n\n" +
            $"Review or manage your subscription:\n{subscriptionsUrl}\n\n" +
            "If you do not want to continue, cancel before the trial ends and you will not be charged.";

        var htmlBody = $"""
            <p>Hi {recipientName},</p>
            <p>Your subscription trial ends on <strong>{trialEndLabel}</strong>.
            When it ends, your plan (<strong>{amountLabel}</strong>) starts automatically and your access continues without interruption.</p>
            <p><a href="{subscriptionsUrl}">Review or manage your subscription</a></p>
            <p>If you do not want to continue, cancel before the trial ends and you will not be charged.</p>
            """;

        var (plain, html) = MergeFooter(plainTextBody, htmlBody, footerService.Build(notification));

        return Task.FromResult<EmailMessage?>(new EmailMessage(
            metadata.RecipientEmail,
            subject,
            plain,
            html,
            recipientName));
    }

    private static TrialWillEndEmailMetadata ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            throw new InvalidOperationException("Trial-ending notification is missing metadata.");
        }

        try
        {
            return JsonSerializer.Deserialize<TrialWillEndEmailMetadata>(metadataJson, MetadataOptions)
                ?? throw new InvalidOperationException("Trial-ending notification metadata could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Trial-ending notification metadata is malformed.", ex);
        }
    }

    private string ResolveConsoleBaseUrl() =>
        (configuration["TrialEmails:ConsoleBaseUrl"]
            ?? configuration["StatementEmails:ConsoleBaseUrl"]
            ?? configuration["InvoiceEmails:ConsoleBaseUrl"]
            ?? configuration["NEXTAUTH_URL"]
            ?? configuration["NEXT_PUBLIC_URL"]
            ?? "http://localhost:3000")
        .Trim()
        .TrimEnd('/');

    private static string BuildAbsoluteUrl(string baseUrl, string relativePath)
        => new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/", UriKind.Absolute), relativePath.TrimStart('/')).ToString();
}
