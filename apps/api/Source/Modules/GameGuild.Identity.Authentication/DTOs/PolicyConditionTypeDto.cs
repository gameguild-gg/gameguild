namespace GameGuild.Identity.Authentication;

public abstract class PolicyConditionTypeDto
{
    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> SupportedOperators { get; set; } = new List<string>();

    public List<string> RequiredParameters { get; set; } = new List<string>();

    public Dictionary<string, object> Examples { get; set; } = new Dictionary<string, object>();
}
