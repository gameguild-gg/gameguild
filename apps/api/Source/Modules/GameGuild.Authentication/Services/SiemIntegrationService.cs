using System.Text.Json;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;
using GameGuild.Authentication.Models;
using GameGuild.Authentication.Models.Analysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Services;

/// <summary>
///     Service for integrating with SIEM (Security Information and Event Management) systems.
///     Supports multiple SIEM backends through configuration.
/// </summary>
public class SiemIntegrationService : ISiemIntegrationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SiemIntegrationService> _logger;
    private readonly HttpClient? _httpClient;
    private readonly bool _isEnabled;
    private readonly string? _siemEndpoint;
    private readonly string? _siemApiKey;

    public SiemIntegrationService(
        IConfiguration configuration,
        ILogger<SiemIntegrationService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _configuration = configuration;
        _logger = logger;

        // Check if SIEM integration is enabled
        _isEnabled = configuration.GetValue<bool>("Authentication:Siem:Enabled");
        _siemEndpoint = configuration.GetValue<string>("Authentication:Siem:Endpoint");
        _siemApiKey = configuration.GetValue<string>("Authentication:Siem:ApiKey");

        if (_isEnabled && httpClientFactory != null)
        {
            _httpClient = httpClientFactory.CreateClient("SiemClient");
        }

        if (_isEnabled && string.IsNullOrEmpty(_siemEndpoint))
        {
            _logger.LogWarning("SIEM integration is enabled but no endpoint is configured. SIEM events will only be logged.");
        }
    }

    public bool IsEnabled() => _isEnabled;

    public async Task SendSecurityEventAsync(SiemEvent siemEvent, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            // Log the event locally
            _logger.LogWarning(
                "SIEM Event: Type={EventType}, Severity={Severity}, UserId={UserId}, IP={IpAddress}, Description={Description}",
                siemEvent.EventType,
                siemEvent.Severity,
                siemEvent.UserId,
                siemEvent.IpAddress,
                siemEvent.Description
            );

            // Send to SIEM endpoint if configured
            if (!string.IsNullOrEmpty(_siemEndpoint) && _httpClient != null)
            {
                await SendToSiemEndpointAsync(siemEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send security event to SIEM system");
        }
    }

    public async Task SendAnomalyEventAsync(AuthenticationAttempt attempt, AuthenticationAttemptAnalysis analysis, CancellationToken cancellationToken = default)
    {
        var siemEvent = new SiemEvent
        {
            EventType = "AuthenticationAnomaly",
            Severity = MapRiskScoreToSeverity(analysis.RiskScore),
            UserId = attempt.UserId,
            IpAddress = attempt.IpAddress,
            UserAgent = attempt.UserAgent,
            Description = $"Anomalous authentication attempt detected with risk score {analysis.RiskScore}",
            RiskScore = analysis.RiskScore,
            TenantId = attempt.TenantId,
            CorrelationId = string.IsNullOrEmpty(attempt.CorrelationId) ? null : Guid.TryParse(attempt.CorrelationId, out var corrId) ? corrId : null,
            Metadata = new Dictionary<string, object>
            {
                ["email"] = attempt.Email,
                ["isSuccessful"] = attempt.IsSuccessful,
                ["failureReason"] = attempt.FailureReason ?? "N/A",
                ["riskFactors"] = analysis.RiskFactors,
                ["location"] = attempt.Location ?? "Unknown",
                ["deviceFingerprint"] = attempt.DeviceFingerprint ?? "N/A"
            }
        };

        await SendSecurityEventAsync(siemEvent, cancellationToken);
    }

    public async Task SendSuspiciousActivityEventAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default)
    {
        var siemEvent = new SiemEvent
        {
            EventType = "SuspiciousActivity",
            Severity = activity.RiskLevel switch
            {
                RiskLevel.Critical => SiemSeverity.Critical,
                RiskLevel.High => SiemSeverity.High,
                RiskLevel.Medium => SiemSeverity.Medium,
                _ => SiemSeverity.Low
            },
            UserId = activity.UserId,
            IpAddress = activity.IpAddress,
            UserAgent = activity.UserAgent,
            Description = $"Suspicious activity detected: {activity.ActivityType}",
            RiskScore = (int)activity.RiskScore,
            Metadata = new Dictionary<string, object>
            {
                ["activityType"] = activity.ActivityType,
                ["identifier"] = activity.Identifier ?? "N/A",
                ["description"] = activity.Description,
                ["occurredAt"] = activity.OccurredAt,
                ["isConfirmedMalicious"] = activity.IsConfirmedMalicious ?? false,
                ["actionsTaken"] = activity.ActionsTaken
            }
        };

        await SendSecurityEventAsync(siemEvent, cancellationToken);
    }

    public async Task SendBruteForceEventAsync(string identifier, int attemptCount, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var siemEvent = new SiemEvent
        {
            EventType = "BruteForceAttack",
            Severity = attemptCount > 20 ? SiemSeverity.Critical : SiemSeverity.High,
            Description = $"Brute force attack detected: {attemptCount} failed attempts in {timeWindow.TotalMinutes:F1} minutes",
            RiskScore = Math.Min(100, attemptCount * 10),
            Metadata = new Dictionary<string, object>
            {
                ["identifier"] = identifier,
                ["attemptCount"] = attemptCount,
                ["timeWindowMinutes"] = timeWindow.TotalMinutes
            }
        };

        await SendSecurityEventAsync(siemEvent, cancellationToken);
    }

    public async Task SendImpossibleTravelEventAsync(Guid userId, LocationInfo fromLocation, LocationInfo toLocation, TimeSpan timeBetween, CancellationToken cancellationToken = default)
    {
        var siemEvent = new SiemEvent
        {
            EventType = "ImpossibleTravel",
            Severity = SiemSeverity.High,
            UserId = userId,
            Description = $"Impossible travel detected: from {fromLocation.Country} to {toLocation.Country} in {timeBetween.TotalHours:F1} hours",
            RiskScore = 85,
            Metadata = new Dictionary<string, object>
            {
                ["fromCountry"] = fromLocation.Country ?? "Unknown",
                ["fromCity"] = fromLocation.City ?? "Unknown",
                ["fromLatitude"] = fromLocation.Latitude?.ToString() ?? "N/A",
                ["fromLongitude"] = fromLocation.Longitude?.ToString() ?? "N/A",
                ["toCountry"] = toLocation.Country ?? "Unknown",
                ["toCity"] = toLocation.City ?? "Unknown",
                ["toLatitude"] = toLocation.Latitude?.ToString() ?? "N/A",
                ["toLongitude"] = toLocation.Longitude?.ToString() ?? "N/A",
                ["timeBetweenHours"] = timeBetween.TotalHours
            }
        };

        await SendSecurityEventAsync(siemEvent, cancellationToken);
    }

    private async Task SendToSiemEndpointAsync(SiemEvent siemEvent, CancellationToken cancellationToken)
    {
        if (_httpClient == null || string.IsNullOrEmpty(_siemEndpoint))
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(siemEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var request = new HttpRequestMessage(HttpMethod.Post, _siemEndpoint)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            // Add API key if configured
            if (!string.IsNullOrEmpty(_siemApiKey))
            {
                request.Headers.Add("X-API-Key", _siemApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send event to SIEM endpoint. Status: {StatusCode}, Event: {EventType}",
                    response.StatusCode,
                    siemEvent.EventType
                );
            }
            else
            {
                _logger.LogDebug("Successfully sent {EventType} event to SIEM system", siemEvent.EventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event to SIEM endpoint");
        }
    }

    private static SiemSeverity MapRiskScoreToSeverity(int riskScore)
    {
        return riskScore switch
        {
            >= 90 => SiemSeverity.Critical,
            >= 70 => SiemSeverity.High,
            >= 40 => SiemSeverity.Medium,
            >= 20 => SiemSeverity.Low,
            _ => SiemSeverity.Info
        };
    }
}
