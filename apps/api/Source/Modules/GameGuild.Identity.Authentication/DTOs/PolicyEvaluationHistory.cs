namespace GameGuild.Identity.Authentication;

public abstract class PolicyEvaluationHistory
{
    public Guid Id { get; set; }

    public DateTime Timestamp { get; set; }

    public Guid? UserId { get; set; }

    public bool Matched { get; set; }

    public string? MatchReason { get; set; }

    public double EvaluationTime { get; set; }

    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}
