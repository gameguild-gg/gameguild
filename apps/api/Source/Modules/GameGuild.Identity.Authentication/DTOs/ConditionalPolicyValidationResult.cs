namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyValidationResult
{
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = new List<string>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Suggestions { get; set; } = new List<string>();

    public Dictionary<string, object> ValidationDetails { get; set; } = new Dictionary<string, object>();
}
