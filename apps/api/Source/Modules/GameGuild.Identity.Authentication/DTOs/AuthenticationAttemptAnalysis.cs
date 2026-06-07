namespace GameGuild.Identity.Authentication;

/// <summary>
///     Analysis result for an authentication attempt
/// </summary>
public class AuthenticationAttemptAnalysis
{
    public bool IsSuspicious { get; set; }

    public int RiskScore { get; set; }

    public List<string> RiskFactors { get; set; } = new List<string>();
}
