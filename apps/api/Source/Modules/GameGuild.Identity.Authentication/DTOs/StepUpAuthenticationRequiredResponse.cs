using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Response indicating that step-up authentication is required.
/// </summary>
public class StepUpAuthenticationRequiredResponse : ProblemDetails
{
    public StepUpAuthenticationRequiredResponse()
    {
        Status = StatusCodes.Status403Forbidden;
        Title = "Fresh verification is required.";
    }

    /// <summary>
    ///     Indicates step-up auth is required.
    /// </summary>
    public bool StepUpRequired { get; set; } = true;

    /// <summary>Persisted operation-bound challenge; this identifier is not an authorization receipt.</summary>
    public Guid ChallengeId { get; set; }

    /// <summary>The server-derived operation whose receipt the protected action requires.</summary>
    public StepUpOperationBinding? Operation { get; set; }

    /// <summary>
    ///     The risk level that triggered step-up requirement.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Reason for step-up requirement.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    ///     Enrolled methods supported by operation step-up (TOTP, backup code, WebAuthn).
    /// </summary>
    public List<string> AvailableMethods { get; set; } = new();

    /// <summary>
    ///     Challenge identifier retained for existing clients; verification still requires an enrolled factor.
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
