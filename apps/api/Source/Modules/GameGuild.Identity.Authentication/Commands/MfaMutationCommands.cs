using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record InitiateMfaSetupCommand(Guid UserId, string UserEmail) : ICommand<MfaSetupResult>;
public sealed record CompleteMfaSetupCommand(Guid UserId, string Code) : ICommand<MfaVerificationResult>;
public sealed record VerifyMfaCodeCommand(Guid UserId, string Code, MfaMethod Method) : ICommand<MfaVerificationResult>;

/// <summary>
///     Issue #1153: regenerating backup codes rotates the user's emergency-access
///     credentials, so it requires the account password (same step-up as MFA disable,
///     issue #244). An empty result array means the re-verification refused the mutation
///     — issued backup-code sets are never legitimately empty.
/// </summary>
public sealed record RegenerateMfaBackupCodesCommand(Guid UserId, string Password) : ICommand<string[]>;

// Issue #864: the payload is the account password re-verified by MfaService.DisableMfaAsync
// (issue #244 hardening) — the legacy "ConfirmationCode" name invited contract churn that
// broke the disable flow once already (Password -> Code -> Password); keep the chain named
// for what it actually carries.
public sealed record DisableMfaCommand(Guid UserId, string Password) : ICommand<bool>;

public sealed class MfaMutationCommandHandler(
    IMfaService mfaService,
    IMfaReverificationService? mfaReverification = null) :
    ICommandHandler<InitiateMfaSetupCommand, MfaSetupResult>,
    ICommandHandler<CompleteMfaSetupCommand, MfaVerificationResult>,
    ICommandHandler<VerifyMfaCodeCommand, MfaVerificationResult>,
    ICommandHandler<RegenerateMfaBackupCodesCommand, string[]>,
    ICommandHandler<DisableMfaCommand, bool>
{
    public Task<MfaSetupResult> Handle(InitiateMfaSetupCommand command, CancellationToken cancellationToken) =>
        mfaService.InitiateMfaSetupAsync(command.UserId, command.UserEmail, cancellationToken);

    public Task<MfaVerificationResult> Handle(CompleteMfaSetupCommand command, CancellationToken cancellationToken) =>
        mfaService.CompleteMfaSetupAsync(command.UserId, command.Code, cancellationToken);

    public Task<MfaVerificationResult> Handle(VerifyMfaCodeCommand command, CancellationToken cancellationToken) =>
        mfaService.VerifyMfaAsync(command.UserId, command.Code, command.Method, cancellationToken);

    public async Task<string[]> Handle(RegenerateMfaBackupCodesCommand command, CancellationToken cancellationToken)
    {
        // Issue #1153: a bearer token alone must not rotate recovery codes (it could
        // burn the real user's emergency access). Fail closed without the verifier.
        if (mfaReverification is null
            || !await mfaReverification.VerifyPasswordAsync(command.UserId, command.Password, "backup-codes-regenerate", cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        return await mfaService.GenerateBackupCodesAsync(command.UserId, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> Handle(DisableMfaCommand command, CancellationToken cancellationToken) =>
        mfaService.DisableMfaAsync(command.UserId, command.Password, cancellationToken);
}
