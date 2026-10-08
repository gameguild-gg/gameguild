namespace GameGuild.Identity.Authentication;

/// <summary>Evaluates MFA against server-resolved memberships for a verified first-factor subject.</summary>
public interface IMfaSubjectRequirementPolicy
{
    Task<MfaRequirementDecision> EvaluateAsync(
        Guid subjectId,
        Guid? requestedTenantId,
        CancellationToken cancellationToken = default);
}
