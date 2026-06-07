namespace GameGuild.Identity.Authentication;

public abstract class TopPolicyUser
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int EvaluationCount { get; set; }

    public DateTime LastEvaluation { get; set; }
}
