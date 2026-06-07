namespace GameGuild.Identity.Authentication;

public abstract class ConditionValidationResult
{
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = new List<string>();

    public List<string> Warnings { get; set; } = new List<string>();

    public string? SuggestedCorrection { get; set; }
}
