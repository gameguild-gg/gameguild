using GameGuild.Identity.Users;

namespace GameGuild.Identity.Authentication;

/// <summary>Server-owned decision before ordinary credential issuance; never a completed MFA proof.</summary>
public sealed class SignInMfaPreparation
{
    private SignInMfaPreparation(SignInResponse? outcome, User? subject = null, MfaRequirementDecision? decision = null)
    {
        Outcome = outcome;
        Subject = subject;
        Decision = decision;
    }

    public SignInResponse? Outcome { get; }

    public bool MayIssueOrdinaryCredentials => Outcome is null && Subject is not null && Decision is not null;

    /// <summary>
    /// True when the current policy still requires MFA but credentials are issued anyway because the
    /// requirement comes only from an elevated role and the subject has no enrolled factor. Dropping
    /// MFA is honored at sign-in; compliance is surfaced to the frontend instead of blocking sign-in.
    /// </summary>
    public bool MfaCompliancePending { get; private set; }

    internal User? Subject { get; }

    internal MfaRequirementDecision? Decision { get; }

    internal static SignInMfaPreparation PermitWithoutMfa(User subject, MfaRequirementDecision decision,
        bool mfaCompliancePending = false)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.SubjectId != subject.Id || decision.TenantId == Guid.Empty)
        {
            throw new ArgumentException("Ordinary credential preparation requires a matching optional MFA policy.", nameof(decision));
        }
        if (!mfaCompliancePending && decision.RequiresMfa)
        {
            throw new ArgumentException("Ordinary credential preparation requires a matching optional MFA policy.", nameof(decision));
        }
        return new SignInMfaPreparation(null, subject, decision) { MfaCompliancePending = mfaCompliancePending };
    }

    internal static SignInMfaPreparation WithOutcome(SignInResponse outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Success || !string.IsNullOrEmpty(outcome.AccessToken) || !string.IsNullOrEmpty(outcome.RefreshToken) || outcome.SessionId != Guid.Empty)
        {
            throw new ArgumentException("A preparation outcome cannot contain ordinary credentials.", nameof(outcome));
        }
        return new SignInMfaPreparation(outcome);
    }
}
