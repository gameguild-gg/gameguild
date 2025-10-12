namespace GameGuild.Modules.Authentication;

/// <summary>
/// Analysis result for a login attempt
/// </summary>
public class AuthenticationAttemptAnalysis
{
    public int RiskScore { get; set; }

    public bool IsSuspicious { get; set; }

    public List<string> RiskFactors { get; set; } = [];

    public DateTime AnalyzedAt { get; set; }
}
