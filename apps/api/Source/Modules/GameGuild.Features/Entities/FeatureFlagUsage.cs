using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Features;

/// <summary>
///     Represents feature flag usage analytics and metrics
/// </summary>
[Table("FeatureFlagUsage")]
public sealed class FeatureFlagUsage : EntityBase
{
    /// <summary>
    ///     Feature flag being tracked
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Tenant ID if tenant-specific usage
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    ///     User ID if user-specific usage
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Environment where the feature was accessed
    /// </summary>
    [MaxLength(50)]
    public string Environment { get; set; } = "production";

    /// <summary>
    ///     Number of times the feature was accessed
    /// </summary>
    public long AccessCount { get; set; } = 1;

    /// <summary>
    ///     Whether the feature was enabled when accessed
    /// </summary>
    public bool WasEnabled { get; set; }

    /// <summary>
    ///     Value returned by the feature flag
    /// </summary>
    [MaxLength(1000)]
    public string? ReturnedValue { get; set; }

    /// <summary>
    ///     Date of first access
    /// </summary>
    public DateTime FirstAccessAt { get; set; }

    /// <summary>
    ///     Date of last access
    /// </summary>
    public DateTime LastAccessAt { get; set; }

    /// <summary>
    ///     Additional context data (JSON)
    /// </summary>
    [MaxLength(2000)]
    public string? ContextData { get; set; }

    /// <summary>
    ///     Navigation property to the feature flag
    /// </summary>
    public FeatureFlag? FeatureFlag { get; set; }
}
