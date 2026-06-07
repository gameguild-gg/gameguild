namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyTestResult
{
    public bool IsValid { get; set; }

    public bool? RuleResult { get; set; }

    public List<string> ValidationErrors { get; set; } = new List<string>();

    public double EvaluationTime { get; set; }

    public string? ErrorMessage { get; set; }

    public Dictionary<string, object> RuleContext { get; set; } = new Dictionary<string, object>();
}
