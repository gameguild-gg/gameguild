namespace GameGuild.Authentication.DTOs;

public abstract class ConditionalPolicyResult
{
    public bool IsApplicable { get; set; }

    public string Decision { get; set; } = string.Empty; // "Allow", "Deny", "RequireAdditional"

    public List<ConditionalPolicyMatch> MatchedPolicies { get; set; } = new List<ConditionalPolicyMatch>();

    public List<string> AdditionalRequirements { get; set; } = new List<string>();

    public Dictionary<string, object> CustomAttributes { get; set; } = new Dictionary<string, object>();

    public DateTime EvaluatedAt { get; set; }
}
