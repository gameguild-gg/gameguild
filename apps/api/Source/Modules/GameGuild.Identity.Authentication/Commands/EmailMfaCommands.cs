using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

// ── Email OTP factor (issue #1152) ───────────────────────────────────────────

/// <summary>Starts email MFA enrollment by sending a verification code to the account's email address.</summary>
public sealed record StartEmailMfaEnrollmentCommand(Guid UserId) : ICommand<EmailMfaChallengeResult>;

/// <summary>Completes email MFA enrollment by verifying the code sent to the pending address.</summary>
public sealed record CompleteEmailMfaEnrollmentCommand(Guid UserId, string Code) : ICommand<MfaVerificationResult>;

/// <summary>Sends a fresh email sign-in challenge to the user's enrolled address (throttled).</summary>
public sealed record SendEmailMfaChallengeCommand(Guid UserId) : ICommand<EmailMfaChallengeResult>;

/// <summary>Handlers for the email factor commands.</summary>
public sealed class EmailMfaCommandHandler(IEmailMfaService emailMfaService) :
    ICommandHandler<StartEmailMfaEnrollmentCommand, EmailMfaChallengeResult>,
    ICommandHandler<CompleteEmailMfaEnrollmentCommand, MfaVerificationResult>,
    ICommandHandler<SendEmailMfaChallengeCommand, EmailMfaChallengeResult>
{
    public Task<EmailMfaChallengeResult> Handle(StartEmailMfaEnrollmentCommand command, CancellationToken cancellationToken) =>
        emailMfaService.StartEnrollmentAsync(command.UserId, cancellationToken);

    public Task<MfaVerificationResult> Handle(CompleteEmailMfaEnrollmentCommand command, CancellationToken cancellationToken) =>
        emailMfaService.CompleteEnrollmentAsync(command.UserId, command.Code, cancellationToken);

    public Task<EmailMfaChallengeResult> Handle(SendEmailMfaChallengeCommand command, CancellationToken cancellationToken) =>
        emailMfaService.SendChallengeAsync(command.UserId, cancellationToken);
}
