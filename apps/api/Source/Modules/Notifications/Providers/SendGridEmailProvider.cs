using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GameGuild.Modules.Notifications.Providers;

/// <summary>
/// Email notification provider interface.
/// </summary>
public interface IEmailNotificationProvider
{
    Task<EmailDeliveryResult> SendAsync(
        string[] recipients,
        string subject,
        string htmlBody,
        string? textBody = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<BulkEmailDeliveryResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyEmailAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// SendGrid email provider implementation.
/// </summary>
public sealed class SendGridEmailProvider : IEmailNotificationProvider
{
    private readonly ISendGridClient _client;
    private readonly ILogger<SendGridEmailProvider> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailProvider(
        ISendGridClient client,
        ILogger<SendGridEmailProvider> logger,
        string fromEmail,
        string fromName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
        _fromName = fromName ?? throw new ArgumentNullException(nameof(fromName));
    }

    public async Task<EmailDeliveryResult> SendAsync(
        string[] recipients,
        string subject,
        string htmlBody,
        string? textBody = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var tos = recipients.Select(r => new EmailAddress(r)).ToList();

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                from,
                tos,
                subject,
                textBody ?? htmlBody,
                htmlBody);

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    msg.AddCustomArg(kvp.Key, kvp.Value);
                }
            }

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            var success = response.IsSuccessStatusCode;

            if (!success)
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SendGrid email failed: {StatusCode} - {Body}", response.StatusCode, body);
            }

            return new EmailDeliveryResult
            {
                Success = success,
                MessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault(),
                Recipients = recipients,
                SentAt = DateTime.UtcNow,
                ErrorMessage = success ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid");
            return new EmailDeliveryResult
            {
                Success = false,
                Recipients = recipients,
                SentAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BulkEmailDeliveryResult> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailDeliveryResult>();
        var tasks = messages.Select(msg => SendAsync(
            msg.Recipients,
            msg.Subject,
            msg.HtmlBody,
            msg.TextBody,
            msg.Metadata,
            cancellationToken));

        var deliveryResults = await Task.WhenAll(tasks);
        results.AddRange(deliveryResults);

        return new BulkEmailDeliveryResult
        {
            TotalSent = results.Count(r => r.Success),
            TotalFailed = results.Count(r => !r.Success),
            Results = results
        };
    }

    public Task<bool> VerifyEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // SendGrid doesn't have a built-in email verification API
        // Use basic email format validation
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return Task.FromResult(addr.Address == email);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// Email message model.
/// </summary>
public sealed class EmailMessage
{
    public required string[] Recipients { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Email delivery result.
/// </summary>
public sealed class EmailDeliveryResult
{
    public required bool Success { get; init; }
    public string? MessageId { get; init; }
    public required string[] Recipients { get; init; }
    public required DateTime SentAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Bulk email delivery result.
/// </summary>
public sealed class BulkEmailDeliveryResult
{
    public required int TotalSent { get; init; }
    public required int TotalFailed { get; init; }
    public required IEnumerable<EmailDeliveryResult> Results { get; init; }
}
