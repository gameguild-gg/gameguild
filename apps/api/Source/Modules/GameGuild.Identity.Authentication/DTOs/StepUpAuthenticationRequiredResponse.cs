namespace GameGuild.Identity.Authentication;

/// <summary>
///     Response indicating that step-up authentication is required.
/// </summary>
public class StepUpAuthenticationRequiredResponse
{
    /// <summary>
    ///     Indicates step-up auth is required.
    /// </summary>
    public bool StepUpRequired { get; set; } = true;

    /// <summary>
    ///     The risk level that triggered step-up requirement.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Reason for step-up requirement.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    ///     Available step-up methods (MFA, Email OTP, SMS OTP, etc.)
    /// </summary>
    public List<string> AvailableMethods { get; set; } = new();

    /// <summary>
    ///     Temporary token for completing step-up authentication.
    /// </summary>
    public string StepUpToken { get; set; } = string.Empty;

    /// <summary>
    ///     Risk factors that triggered the requirement.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    ///     Expiration time for the step-up token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
