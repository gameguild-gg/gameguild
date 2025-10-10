using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GameGuild.Modules.Notifications.Providers;

/// <summary>
/// SMS notification provider interface.
/// </summary>
public interface ISmsNotificationProvider
{
    Task<SmsDeliveryResult> SendAsync(
        string[] phoneNumbers,
        string message,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<BulkSmsDeliveryResult> SendBulkAsync(
        IEnumerable<SmsMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Twilio SMS provider implementation.
/// </summary>
public sealed class TwilioSmsProvider : ISmsNotificationProvider
{
    private readonly ILogger<TwilioSmsProvider> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;

    public TwilioSmsProvider(
        ILogger<TwilioSmsProvider> logger,
        string accountSid,
        string authToken,
        string fromPhoneNumber)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _accountSid = accountSid ?? throw new ArgumentNullException(nameof(accountSid));
        _authToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
        _fromPhoneNumber = fromPhoneNumber ?? throw new ArgumentNullException(nameof(fromPhoneNumber));

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<SmsDeliveryResult> SendAsync(
        string[] phoneNumbers,
        string message,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SingleSmsDeliveryResult>();

        foreach (var phoneNumber in phoneNumbers)
        {
            try
            {
                var messageResource = await MessageResource.CreateAsync(
                    to: new PhoneNumber(phoneNumber),
                    from: new PhoneNumber(_fromPhoneNumber),
                    body: message);

                results.Add(new SingleSmsDeliveryResult
                {
                    Success = messageResource.Status != MessageResource.StatusEnum.Failed,
                    MessageId = messageResource.Sid,
                    PhoneNumber = phoneNumber,
                    SentAt = DateTime.UtcNow,
                    Status = messageResource.Status.ToString()
                });

                if (messageResource.Status == MessageResource.StatusEnum.Failed)
                {
                    _logger.LogError("Twilio SMS failed for {PhoneNumber}: {ErrorMessage}",
                        phoneNumber, messageResource.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber} via Twilio", phoneNumber);
                results.Add(new SingleSmsDeliveryResult
                {
                    Success = false,
                    PhoneNumber = phoneNumber,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });
            }
        }

        return new SmsDeliveryResult
        {
            TotalSent = results.Count(r => r.Success),
            TotalFailed = results.Count(r => !r.Success),
            Results = results
        };
    }

    public async Task<BulkSmsDeliveryResult> SendBulkAsync(
        IEnumerable<SmsMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<SingleSmsDeliveryResult>();

        foreach (var msg in messages)
        {
            var result = await SendAsync(msg.PhoneNumbers, msg.Message, msg.Metadata, cancellationToken);
            allResults.AddRange(result.Results);
        }

        return new BulkSmsDeliveryResult
        {
            TotalSent = allResults.Count(r => r.Success),
            TotalFailed = allResults.Count(r => !r.Success),
            Results = allResults
        };
    }

    public async Task<bool> VerifyPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use Twilio Lookup API to verify phone number
            var lookup = await Twilio.Rest.Lookups.V1.PhoneNumberResource.FetchAsync(
                pathPhoneNumber: new PhoneNumber(phoneNumber));

            return lookup != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Phone number verification failed for {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}

/// <summary>
/// SMS message model.
/// </summary>
public sealed class SmsMessage
{
    public required string[] PhoneNumbers { get; init; }
    public required string Message { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// SMS delivery result.
/// </summary>
public sealed class SmsDeliveryResult
{
    public required int TotalSent { get; init; }
    public required int TotalFailed { get; init; }
    public required IEnumerable<SingleSmsDeliveryResult> Results { get; init; }
}

/// <summary>
/// Single SMS delivery result.
/// </summary>
public sealed class SingleSmsDeliveryResult
{
    public required bool Success { get; init; }
    public string? MessageId { get; init; }
    public required string PhoneNumber { get; init; }
    public required DateTime SentAt { get; init; }
    public string? Status { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Bulk SMS delivery result.
/// </summary>
public sealed class BulkSmsDeliveryResult
{
    public required int TotalSent { get; init; }
    public required int TotalFailed { get; init; }
    public required IEnumerable<SingleSmsDeliveryResult> Results { get; init; }
}
