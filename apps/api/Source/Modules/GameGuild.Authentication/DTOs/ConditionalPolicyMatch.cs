namespace GameGuild.Authentication.DTOs;

public abstract class ConditionalPolicyMatch
{
    public Guid PolicyId { get; set; }

    public string PolicyName { get; set; } = string.Empty;

    public int Priority { get; set; }

    public string Action { get; set; } = string.Empty;

    public bool Matched { get; set; }

    public string? MatchReason { get; set; }
}
