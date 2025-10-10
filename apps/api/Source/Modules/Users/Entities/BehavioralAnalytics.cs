using GameGuild.Core.Domain;

namespace GameGuild.Modules.Users.Entities;

/// <summary>
/// Represents a user behavior event for analytics and profile enrichment.
/// </summary>
public sealed class UserBehaviorEvent : EntityBase
{
    /// <summary>
    /// Gets or sets the user ID associated with this event.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the type of event (e.g., "PageView", "Click", "Purchase", "Login").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets event-specific properties (JSON format).
    /// </summary>
    public string Properties { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the session ID this event belongs to.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the source of the event (e.g., "Web", "Mobile", "API").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the page/screen where the event occurred.
    /// </summary>
    public string? Page { get; set; }

    /// <summary>
    /// Gets or sets the user's IP address at the time of the event.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the referrer URL.
    /// </summary>
    public string? Referrer { get; set; }

    /// <summary>
    /// Gets or sets when this event expires and can be purged.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether this event has been processed for enrichment.
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Gets or sets when this event was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Represents an enriched profile attribute extracted from user behavior.
/// </summary>
public sealed class ProfileAttribute : EntityBase
{
    /// <summary>
    /// Gets or sets the user ID this attribute belongs to.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the attribute key (e.g., "PreferredCategory", "ActivityLevel", "TimeZone").
    /// </summary>
    public string AttributeKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attribute value.
    /// </summary>
    public string AttributeValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of this attribute (e.g., "BehaviorAnalysis", "ManualInput", "ThirdParty").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0) indicating how confident we are in this attribute.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets when this attribute was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when this attribute expires and should be recalculated.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times this attribute has been recalculated.
    /// </summary>
    public int RecalculationCount { get; set; }

    /// <summary>
    /// Gets or sets supporting metadata for this attribute (JSON format).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the previous value before the last update.
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Gets whether this attribute is high confidence (>= 0.8).
    /// </summary>
    public bool IsHighConfidence => Confidence >= 0.8;

    /// <summary>
    /// Gets whether this attribute has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Updates the attribute with a new value and confidence score.
    /// </summary>
    public void Update(string newValue, double confidence, string? metadata = null)
    {
        PreviousValue = AttributeValue;
        AttributeValue = newValue;
        Confidence = confidence;
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
        RecalculationCount++;
    }

    /// <summary>
    /// Sets the expiration date for this attribute.
    /// </summary>
    public void SetExpiration(TimeSpan duration)
    {
        ExpiresAt = DateTime.UtcNow.Add(duration);
    }
}

/// <summary>
/// Attribute confidence level categorization.
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>
    /// Very low confidence (0.0 - 0.3)
    /// </summary>
    VeryLow = 0,

    /// <summary>
    /// Low confidence (0.3 - 0.5)
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence (0.5 - 0.7)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence (0.7 - 0.9)
    /// </summary>
    High = 3,

    /// <summary>
    /// Very high confidence (0.9 - 1.0)
    /// </summary>
    VeryHigh = 4
}
