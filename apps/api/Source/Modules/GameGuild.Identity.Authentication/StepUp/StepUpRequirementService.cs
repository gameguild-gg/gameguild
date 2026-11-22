using GameGuild.CQRS;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authentication;

public interface IStepUpRequirementService
{
    Task<StepUpAuthenticationRequiredResponse> CreateAsync(StepUpOperationBinding operation);

    Task<StepUpAuthenticationRequiredResponse> CreateAsync(
        StepUpOperationBinding operation,
        CancellationToken cancellationToken);
}

/// <summary>Creates the operation-bound verification demand for a protected action.</summary>
public sealed class StepUpRequirementService(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    IUserMfaConfigurationRepository mfaConfigurations,
    IWebAuthnCredentialRepository webAuthnCredentials,
    MfaOptions mfaOptions) : IStepUpRequirementService
{
    public Task<StepUpAuthenticationRequiredResponse> CreateAsync(StepUpOperationBinding operation) =>
        CreateAsync(operation, CancellationToken.None);

    public async Task<StepUpAuthenticationRequiredResponse> CreateAsync(
        StepUpOperationBinding operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } actorId || actorId == Guid.Empty ||
            actor.TenantId is not { } tenantId || tenantId == Guid.Empty ||
            !Guid.TryParse(actor.TypedAttributes.SessionId, out var sessionId) || sessionId == Guid.Empty)
        {
            throw new StepUpContextUnavailableException("An authenticated tenant and session context is required.");
        }

        // Availability follows the strict verifier's enrollment policy without changing legacy MFA status.
        var configuration = mfaOptions.Enabled
            ? await mfaConfigurations.GetByUserIdAsync(actorId, cancellationToken).ConfigureAwait(false)
            : null;
        var methods = new List<string>();
        if (configuration is { IsEnabled: true, IsSetupComplete: true })
        {
            if (!string.IsNullOrEmpty(configuration.TotpSecretKey))
            {
                methods.Add(nameof(MfaMethod.Totp));
            }

            if (BackupCodeSet.Read(configuration.BackupCodes).Hashes.Count > 0)
            {
                methods.Add(nameof(MfaMethod.BackupCode));
            }
        }

        if (await webAuthnCredentials.HasActiveCredentialsAsync(actorId, cancellationToken).ConfigureAwait(false))
        {
            methods.Add(nameof(MfaMethod.WebAuthn));
        }

        var challenge = await sender.Send(new CreateStepUpChallengeCommand(operation), cancellationToken).ConfigureAwait(false);
        return new StepUpAuthenticationRequiredResponse
        {
            StepUpRequired = true,
            RiskLevel = RiskLevel.High,
            Reason = "Fresh verification is required for this sensitive operation.",
            RiskFactors = ["SensitiveOperation"],
            AvailableMethods = methods,
            ChallengeId = challenge.ChallengeId,
            StepUpToken = challenge.ChallengeId.ToString("D"),
            Operation = operation,
            ExpiresAt = challenge.ExpiresAt.UtcDateTime
        };
    }
}
