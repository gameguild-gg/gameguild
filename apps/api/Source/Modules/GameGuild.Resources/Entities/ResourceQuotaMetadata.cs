using System.Text.Json.Serialization;

namespace GameGuild.Resources;

/// <summary>
///     Strongly-typed metadata for resource quotas.
///     Stored as JSON in the database but with compile-time type safety.
/// </summary>
public sealed record ResourceQuotaMetadata
{
    /// <summary>
    ///     Optional custom name for this quota configuration
    /// </summary>
    [JsonPropertyName("customName")]
    public string? CustomName { get; init; }

    /// <summary>
    ///     Email address to notify when quota thresholds are reached
    /// </summary>
    [JsonPropertyName("notificationEmail")]
    public string? NotificationEmail { get; init; }

    /// <summary>
    ///     Whether to send notifications when soft limit is exceeded
    /// </summary>
    [JsonPropertyName("notifyOnSoftLimit")]
    public bool NotifyOnSoftLimit { get; init; }

    /// <summary>
    ///     Whether to send notifications when hard limit is exceeded
    /// </summary>
    [JsonPropertyName("notifyOnHardLimit")]
    public bool NotifyOnHardLimit { get; init; }

    /// <summary>
    ///     Custom warning threshold percentage (0-100)
    /// </summary>
    [JsonPropertyName("warningThresholdPercent")]
    public int? WarningThresholdPercent { get; init; }

    /// <summary>
    ///     Source system or integration that created this quota
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>
    ///     External reference ID for integration purposes
    /// </summary>
    [JsonPropertyName("externalReferenceId")]
    public string? ExternalReferenceId { get; init; }

    /// <summary>
    ///     Optional notes or context about why this quota was configured
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>
    ///     Tags for categorization and filtering
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    ///     Custom key-value pairs for extensibility
    /// </summary>
    [JsonPropertyName("customProperties")]
    public IReadOnlyDictionary<string, string>? CustomProperties { get; init; }

    /// <summary>
    ///     Creates an empty metadata instance
    /// </summary>
    public static ResourceQuotaMetadata Empty => new();
}
