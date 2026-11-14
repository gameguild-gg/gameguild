using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record CreateStepUpChallengeCommand(StepUpOperationBinding Binding) : ICommand<StepUpChallengeResponse>;
public sealed record BeginStepUpWebAuthnCommand(Guid ChallengeId) : ICommand<WebAuthnAuthenticationOptionsResult>;
public sealed record VerifyStepUpChallengeCommand(Guid ChallengeId, StepUpVerification Verification) : ICommand<StepUpVerificationOutcome>;

// This transport-independent outcome is unwrapped by the controller after the
// command transaction completes; it is never an HTTP request or success receipt.
public abstract record StepUpVerificationOutcome(bool Success, StepUpReceiptResponse? Receipt);
internal sealed record StepUpVerificationSucceeded(StepUpReceiptResponse VerifiedReceipt)
    : StepUpVerificationOutcome(true, VerifiedReceipt);
internal sealed record StepUpVerificationRecordedDenial()
    : StepUpVerificationOutcome(false, null), ICommitOnFailureOutcome;

public sealed class StepUpCommandHandler(IStepUpReceiptService service) :
    ICommandHandler<CreateStepUpChallengeCommand, StepUpChallengeResponse>,
    ICommandHandler<BeginStepUpWebAuthnCommand, WebAuthnAuthenticationOptionsResult>,
    ICommandHandler<VerifyStepUpChallengeCommand, StepUpVerificationOutcome>
{
    public Task<StepUpChallengeResponse> Handle(CreateStepUpChallengeCommand command, CancellationToken ct)
        => service.CreateChallengeAsync(command.Binding, ct);

    public Task<WebAuthnAuthenticationOptionsResult> Handle(BeginStepUpWebAuthnCommand command, CancellationToken ct)
        => service.BeginWebAuthnAsync(command.ChallengeId, ct);

    public async Task<StepUpVerificationOutcome> Handle(VerifyStepUpChallengeCommand command, CancellationToken ct)
    {
        try
        {
            var receipt = await service.VerifyAsync(command.ChallengeId, command.Verification, ct).ConfigureAwait(false);
            return new StepUpVerificationSucceeded(receipt);
        }
        catch (StepUpVerificationFailedException)
        {
            // A durable attempt and ordinary factor denial must commit. Exceptions
            // after successful factor consumption (e.g. receipt CAS loss), infrastructure
            // failures and cancellation still escape and roll back the whole transaction.
            return new StepUpVerificationRecordedDenial();
        }
    }
}
