namespace GameGuild.Identity.Authentication;

/// <summary>
///     Outcome of a fresh-verification (sensitive-action assurance) check.
/// </summary>
public sealed record FreshVerificationResult(bool IsVerified, string? FailureReason = null)
{
    public static FreshVerificationResult Verified { get; } = new(true);

    public static FreshVerificationResult Failed(string reason) => new(false, reason);
}

/// <summary>
///     Fresh-verification gate for trusted-device mutations (issue #1144): the acting user's
///     authentication must be recent (<c>auth_time</c> within the configured window), and when
///     the user has MFA enabled the current token must carry a verified MFA proof. This reuses
///     the same sensitive-action assurance semantics as the teams/projects ownership flows.
/// </summary>
public interface IFreshVerificationGate
{
    /// <summary>
    ///     Evaluates the fresh-verification prerequisite for the acting user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verified when the acting session's authentication is fresh enough; otherwise the denial reason.</returns>
    Task<FreshVerificationResult> VerifyRecentAuthenticationAsync(CancellationToken cancellationToken = default);
}
