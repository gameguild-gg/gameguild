using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Notifications.Providers;

/// <summary>
/// Push notification provider interface.
/// </summary>
public interface IPushNotificationProvider
{
    Task<PushDeliveryResult> SendAsync(
        string[] deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<BulkPushDeliveryResult> SendBulkAsync(
        IEnumerable<PushMessage> messages,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyDeviceTokenAsync(string deviceToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Firebase Cloud Messaging (FCM) push notification provider implementation.
/// </summary>
public sealed class FcmPushNotificationProvider : IPushNotificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FcmPushNotificationProvider> _logger;
    private readonly string _serverKey;
    private const string FcmUrl = "https://fcm.googleapis.com/fcm/send";

    public FcmPushNotificationProvider(
        HttpClient httpClient,
        ILogger<FcmPushNotificationProvider> logger,
        string serverKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serverKey = serverKey ?? throw new ArgumentNullException(nameof(serverKey));

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"key={_serverKey}");
    }

    public async Task<PushDeliveryResult> SendAsync(
        string[] deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SinglePushDeliveryResult>();

        foreach (var deviceToken in deviceTokens)
        {
            try
            {
                var payload = new
                {
                    to = deviceToken,
                    notification = new
                    {
                        title,
                        body
                    },
                    data = data ?? new Dictionary<string, string>()
                };

                var response = await _httpClient.PostAsJsonAsync(FcmUrl, payload, cancellationToken);
                var responseContent = await response.Content.ReadFromJsonAsync<FcmResponse>(cancellationToken: cancellationToken);

                var success = response.IsSuccessStatusCode && responseContent?.Success == 1;

                results.Add(new SinglePushDeliveryResult
                {
                    Success = success,
                    MessageId = responseContent?.Results?.FirstOrDefault()?.MessageId,
                    DeviceToken = deviceToken,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = success ? null : responseContent?.Results?.FirstOrDefault()?.Error
                });

                if (!success)
                {
                    _logger.LogError("FCM push failed for device {DeviceToken}: {Error}",
                        deviceToken, responseContent?.Results?.FirstOrDefault()?.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to {DeviceToken} via FCM", deviceToken);
                results.Add(new SinglePushDeliveryResult
                {
                    Success = false,
                    DeviceToken = deviceToken,
                    SentAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });
            }
        }

        return new PushDeliveryResult
        {
            TotalSent = results.Count(r => r.Success),
            TotalFailed = results.Count(r => !r.Success),
            Results = results
        };
    }

    public async Task<BulkPushDeliveryResult> SendBulkAsync(
        IEnumerable<PushMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<SinglePushDeliveryResult>();

        foreach (var msg in messages)
        {
            var result = await SendAsync(
                msg.DeviceTokens,
                msg.Title,
                msg.Body,
                msg.Data,
                msg.Metadata,
                cancellationToken);
            allResults.AddRange(result.Results);
        }

        return new BulkPushDeliveryResult
        {
            TotalSent = allResults.Count(r => r.Success),
            TotalFailed = allResults.Count(r => !r.Success),
            Results = allResults
        };
    }

    public async Task<bool> VerifyDeviceTokenAsync(string deviceToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Send a test notification to verify token
            var payload = new
            {
                to = deviceToken,
                dry_run = true,
                notification = new
                {
                    title = "Token Verification",
                    body = "This is a test"
                }
            };

            var response = await _httpClient.PostAsJsonAsync(FcmUrl, payload, cancellationToken);
            var responseContent = await response.Content.ReadFromJsonAsync<FcmResponse>(cancellationToken: cancellationToken);

            return response.IsSuccessStatusCode && responseContent?.Success == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device token verification failed for {DeviceToken}", deviceToken);
            return false;
        }
    }

    // FCM response models
    private sealed class FcmResponse
    {
        public int Success { get; set; }
        public int Failure { get; set; }
        public List<FcmResult>? Results { get; set; }
    }

    private sealed class FcmResult
    {
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }
}

/// <summary>
/// Push message model.
/// </summary>
public sealed class PushMessage
{
    public required string[] DeviceTokens { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public Dictionary<string, string>? Data { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Push delivery result.
/// </summary>
public sealed class PushDeliveryResult
{
    public required int TotalSent { get; init; }
    public required int TotalFailed { get; init; }
    public required IEnumerable<SinglePushDeliveryResult> Results { get; init; }
}

/// <summary>
/// Single push delivery result.
/// </summary>
public sealed class SinglePushDeliveryResult
{
    public required bool Success { get; init; }
    public string? MessageId { get; init; }
    public required string DeviceToken { get; init; }
    public required DateTime SentAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Bulk push delivery result.
/// </summary>
public sealed class BulkPushDeliveryResult
{
    public required int TotalSent { get; init; }
    public required int TotalFailed { get; init; }
    public required IEnumerable<SinglePushDeliveryResult> Results { get; init; }
}
