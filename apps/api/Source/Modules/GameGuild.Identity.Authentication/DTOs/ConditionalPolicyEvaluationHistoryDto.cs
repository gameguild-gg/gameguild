namespace GameGuild.Identity.Authentication;

public abstract class ConditionalPolicyEvaluationHistoryDto
{
    public Guid PolicyId { get; set; }

    public List<PolicyEvaluationHistory> History { get; set; } = new List<PolicyEvaluationHistory>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
