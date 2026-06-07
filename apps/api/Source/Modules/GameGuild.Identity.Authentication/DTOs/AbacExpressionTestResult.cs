namespace GameGuild.Identity.Authentication;

public abstract class AbacExpressionTestResult
{
    public bool IsValid { get; set; }

    public bool? EvaluationResult { get; set; }

    public List<string> ValidationErrors { get; set; } = new List<string>();

    public double EvaluationTime { get; set; }

    public string? ErrorMessage { get; set; }

    public Dictionary<string, object> DebugInfo { get; set; } = new Dictionary<string, object>();
}
