namespace GameGuild.Features;

/// <summary>
///     Targeting rule for feature flags
/// </summary>
public class TargetingRule
{
    public string TargetType { get; set; } = string.Empty; // "tenant", "user", "plan", "country", "custom"

    public string TargetIdentifier { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public int RolloutPercentage { get; set; } = 100;

    public string? CustomValue { get; set; }

    public int Priority { get; set; }

    public Dictionary<string, object> Conditions { get; init; } = [];
}
