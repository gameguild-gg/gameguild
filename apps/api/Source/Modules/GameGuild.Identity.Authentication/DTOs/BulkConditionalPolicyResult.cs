namespace GameGuild.Identity.Authentication;

public abstract class BulkConditionalPolicyResult
{
    public int TotalEvaluations { get; set; }

    public int SuccessfulEvaluations { get; set; }

    public int FailedEvaluations { get; set; }

    public List<ConditionalPolicyEvaluationFailure> Failures { get; set; } = new List<ConditionalPolicyEvaluationFailure>();

    public double AverageEvaluationTime { get; set; }

    public DateTime ProcessedAt { get; set; }
}
