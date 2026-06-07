namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicySimulationResult
{
    public bool Success { get; set; }

    public List<SimulationStep> Steps { get; set; } = new List<SimulationStep>();

    public string FinalDecision { get; set; } = string.Empty;

    public Dictionary<string, object> FinalContext { get; set; } = new Dictionary<string, object>();

    public double TotalEvaluationTime { get; set; }

    public List<string> Warnings { get; set; } = new List<string>();
}
