namespace GameGuild.Identity.Authentication;

public abstract class PolicyEvaluationMetric
{
    public DateTime Date { get; set; }

    public int EvaluationCount { get; set; }

    public int PositiveCount { get; set; }

    public int NegativeCount { get; set; }

    public double AverageTime { get; set; }
}
