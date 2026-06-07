namespace GameGuild.Identity.Authentication;

public abstract class SimulationStep
{
    public int StepNumber { get; set; }

    public string Action { get; set; } = string.Empty;

    public bool Result { get; set; }

    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}
