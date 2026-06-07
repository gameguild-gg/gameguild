namespace GameGuild.Identity.Authentication;

public abstract class BulkAbacEvaluationResult
{
    public int TotalEvaluations { get; set; }

    public int SuccessfulEvaluations { get; set; }

    public int FailedEvaluations { get; set; }

    public List<AbacEvaluationFailure> Failures { get; set; } = new List<AbacEvaluationFailure>();

    public double AverageEvaluationTime { get; set; }

    public DateTime ProcessedAt { get; set; }
}
