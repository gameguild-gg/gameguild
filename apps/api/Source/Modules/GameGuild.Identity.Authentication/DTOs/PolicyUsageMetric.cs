namespace GameGuild.Identity.Authentication;

public abstract class PolicyUsageMetric
{
    public DateTime Date { get; set; }

    public int EvaluationCount { get; set; }

    public double AverageTime { get; set; }

    public int PolicyCount { get; set; }
}
