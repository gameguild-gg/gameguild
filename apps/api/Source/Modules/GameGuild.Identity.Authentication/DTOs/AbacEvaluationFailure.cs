namespace GameGuild.Identity.Authentication;

public abstract class AbacEvaluationFailure
{
    public int ContextIndex { get; set; }

    public string Error { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
