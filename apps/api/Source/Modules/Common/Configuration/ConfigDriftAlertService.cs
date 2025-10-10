using System;
using GameGuild.Modules.Common.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Common.Configuration;

/// <summary>
/// Configuration drift detection and alerting service
/// </summary>
public sealed class ConfigDriftAlertService : IConfigDriftAlertService
{
    private readonly ILogger<ConfigDriftAlertService> _logger;
    private readonly IConfigVersionService _configVersionService;
    private readonly IAlertingService _alertingService;

    public ConfigDriftAlertService(
        ILogger<ConfigDriftAlertService> logger,
        IConfigVersionService configVersionService,
        IAlertingService alertingService)
    {
        _logger = logger;
        _configVersionService = configVersionService;
        _alertingService = alertingService;
    }

    /// <inheritdoc/>
    public async Task<DriftAlertResult> CheckAndAlertAsync(
        Guid baselineSnapshotId,
        DriftAlertPolicy policy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting drift check against baseline {BaselineId}", baselineSnapshotId);

            // Detect drift
            var drift = await _configVersionService.DetectDriftAsync(baselineSnapshotId, cancellationToken);

            if (!drift.HasDrift)
            {
                _logger.LogInformation("No configuration drift detected");
                return new DriftAlertResult { DriftDetected = false };
            }

            // Evaluate severity
            var severity = EvaluateSeverity(drift, policy);

            _logger.LogWarning(
                "Configuration drift detected: {ChangeCount} changes with severity {Severity}",
                drift.Changes.Count,
                severity);

            // Send alerts based on policy
            var alertsSent = new List<AlertDelivery>();

            if (ShouldAlert(severity, policy))
            {
                // Email alerts
                if (policy.EmailRecipients?.Any() == true)
                {
                    var emailResult = await SendEmailAlertAsync(drift, severity, policy, cancellationToken);
                    alertsSent.Add(emailResult);
                }

                // Slack/Teams webhooks
                if (!string.IsNullOrEmpty(policy.WebhookUrl))
                {
                    var webhookResult = await SendWebhookAlertAsync(drift, severity, policy, cancellationToken);
                    alertsSent.Add(webhookResult);
                }

                // SMS alerts for critical
                if (severity == DriftSeverity.Critical && policy.SmsRecipients?.Any() == true)
                {
                    var smsResult = await SendSmsAlertAsync(drift, severity, policy, cancellationToken);
                    alertsSent.Add(smsResult);
                }

                // PagerDuty for critical
                if (severity == DriftSeverity.Critical && !string.IsNullOrEmpty(policy.PagerDutyKey))
                {
                    var pagerDutyResult = await SendPagerDutyAlertAsync(drift, severity, policy, cancellationToken);
                    alertsSent.Add(pagerDutyResult);
                }
            }

            return new DriftAlertResult
            {
                DriftDetected = true,
                Drift = drift,
                Severity = severity,
                AlertsSent = alertsSent.ToArray(),
                DetectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and alert on configuration drift");
            throw;
        }
    }

    private DriftSeverity EvaluateSeverity(ConfigDrift drift, DriftAlertPolicy policy)
    {
        // Check for critical changes
        var criticalKeys = policy.CriticalConfigKeys ?? Array.Empty<string>();
        if (drift.Changes.Any(c => criticalKeys.Contains(c.Key, StringComparer.OrdinalIgnoreCase)))
        {
            return DriftSeverity.Critical;
        }

        // Check change count thresholds
        if (drift.Changes.Count >= policy.CriticalChangeThreshold)
        {
            return DriftSeverity.Critical;
        }

        if (drift.Changes.Count >= policy.HighChangeThreshold)
        {
            return DriftSeverity.High;
        }

        if (drift.Changes.Count >= policy.MediumChangeThreshold)
        {
            return DriftSeverity.Medium;
        }

        return DriftSeverity.Low;
    }

    private bool ShouldAlert(DriftSeverity severity, DriftAlertPolicy policy)
    {
        return severity switch
        {
            DriftSeverity.Critical => true,
            DriftSeverity.High => policy.AlertOnHigh,
            DriftSeverity.Medium => policy.AlertOnMedium,
            DriftSeverity.Low => policy.AlertOnLow,
            _ => false
        };
    }

    private async Task<AlertDelivery> SendEmailAlertAsync(
        ConfigDrift drift,
        DriftSeverity severity,
        DriftAlertPolicy policy,
        CancellationToken cancellationToken)
    {
        var subject = $"[{severity}] Configuration Drift Detected - {drift.Changes.Count} Changes";
        var body = FormatEmailBody(drift, severity);

        var result = await _alertingService.SendEmailAsync(
            policy.EmailRecipients!,
            subject,
            body,
            cancellationToken);

        return new AlertDelivery
        {
            Channel = "Email",
            Success = result.Success,
            Recipients = policy.EmailRecipients!,
            SentAt = DateTime.UtcNow,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<AlertDelivery> SendWebhookAlertAsync(
        ConfigDrift drift,
        DriftSeverity severity,
        DriftAlertPolicy policy,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            severity = severity.ToString(),
            changeCount = drift.Changes.Count,
            changes = drift.Changes.Take(10).Select(c => new
            {
                key = c.Key,
                oldValue = c.OldValue,
                newValue = c.NewValue,
                changeType = c.ChangeType.ToString()
            }),
            detectedAt = DateTime.UtcNow,
            baselineId = drift.BaselineSnapshotId
        };

        var result = await _alertingService.SendWebhookAsync(
            policy.WebhookUrl!,
            payload,
            cancellationToken);

        return new AlertDelivery
        {
            Channel = "Webhook",
            Success = result.Success,
            Recipients = new[] { policy.WebhookUrl! },
            SentAt = DateTime.UtcNow,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<AlertDelivery> SendSmsAlertAsync(
        ConfigDrift drift,
        DriftSeverity severity,
        DriftAlertPolicy policy,
        CancellationToken cancellationToken)
    {
        var message = $"CRITICAL: Config drift detected - {drift.Changes.Count} changes. Review immediately.";

        var result = await _alertingService.SendSmsAsync(
            policy.SmsRecipients!,
            message,
            cancellationToken);

        return new AlertDelivery
        {
            Channel = "SMS",
            Success = result.Success,
            Recipients = policy.SmsRecipients!,
            SentAt = DateTime.UtcNow,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<AlertDelivery> SendPagerDutyAlertAsync(
        ConfigDrift drift,
        DriftSeverity severity,
        DriftAlertPolicy policy,
        CancellationToken cancellationToken)
    {
        var incident = new
        {
            routing_key = policy.PagerDutyKey,
            event_action = "trigger",
            payload = new
            {
                summary = $"Configuration Drift: {drift.Changes.Count} changes detected",
                severity = "critical",
                source = "ConfigDriftMonitor",
                custom_details = new
                {
                    changeCount = drift.Changes.Count,
                    baselineId = drift.BaselineSnapshotId
                }
            }
        };

        var result = await _alertingService.SendPagerDutyAsync(incident, cancellationToken);

        return new AlertDelivery
        {
            Channel = "PagerDuty",
            Success = result.Success,
            Recipients = new[] { "PagerDuty Incident" },
            SentAt = DateTime.UtcNow,
            ErrorMessage = result.ErrorMessage
        };
    }

    private string FormatEmailBody(ConfigDrift drift, DriftSeverity severity)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<h2>Configuration Drift Detected - Severity: {severity}</h2>");
        sb.AppendLine($"<p><strong>Total Changes:</strong> {drift.Changes.Count}</p>");
        sb.AppendLine($"<p><strong>Detected At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>");
        sb.AppendLine("<h3>Changes:</h3>");
        sb.AppendLine("<table border='1' cellpadding='5' cellspacing='0'>");
        sb.AppendLine("<tr><th>Key</th><th>Change Type</th><th>Old Value</th><th>New Value</th></tr>");

        foreach (var change in drift.Changes.Take(20))
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(change.Key)}</td>");
            sb.AppendLine($"<td>{change.ChangeType}</td>");
            sb.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(change.OldValue)}</td>");
            sb.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(change.NewValue)}</td>");
            sb.AppendLine($"</tr>");
        }

        if (drift.Changes.Count > 20)
        {
            sb.AppendLine($"<tr><td colspan='4'><em>... and {drift.Changes.Count - 20} more changes</em></td></tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }
}

/// <summary>
/// Configuration drift alert policy
/// </summary>
public sealed class DriftAlertPolicy
{
    public string[] CriticalConfigKeys { get; set; } = Array.Empty<string>();
    public int CriticalChangeThreshold { get; set; } = 10;
    public int HighChangeThreshold { get; set; } = 5;
    public int MediumChangeThreshold { get; set; } = 2;
    public bool AlertOnCritical { get; set; } = true;
    public bool AlertOnHigh { get; set; } = true;
    public bool AlertOnMedium { get; set; } = false;
    public bool AlertOnLow { get; set; } = false;
    public string[]? EmailRecipients { get; set; }
    public string[]? SmsRecipients { get; set; }
    public string? WebhookUrl { get; set; }
    public string? PagerDutyKey { get; set; }
}

/// <summary>
/// Drift severity levels
/// </summary>
public enum DriftSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Drift alert result
/// </summary>
public sealed class DriftAlertResult
{
    public bool DriftDetected { get; set; }
    public ConfigDrift? Drift { get; set; }
    public DriftSeverity Severity { get; set; }
    public AlertDelivery[] AlertsSent { get; set; } = Array.Empty<AlertDelivery>();
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Alert delivery record
/// </summary>
public sealed class AlertDelivery
{
    public string Channel { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string[] Recipients { get; set; } = Array.Empty<string>();
    public DateTime SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Alerting service interface
/// </summary>
public interface IAlertingService
{
    Task<AlertResult> SendEmailAsync(string[] recipients, string subject, string body, CancellationToken cancellationToken);
    Task<AlertResult> SendSmsAsync(string[] phoneNumbers, string message, CancellationToken cancellationToken);
    Task<AlertResult> SendWebhookAsync(string webhookUrl, object payload, CancellationToken cancellationToken);
    Task<AlertResult> SendPagerDutyAsync(object incident, CancellationToken cancellationToken);
}

public sealed class AlertResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Config drift alert service interface
/// </summary>
public interface IConfigDriftAlertService
{
    Task<DriftAlertResult> CheckAndAlertAsync(Guid baselineSnapshotId, DriftAlertPolicy policy, CancellationToken cancellationToken = default);
}
