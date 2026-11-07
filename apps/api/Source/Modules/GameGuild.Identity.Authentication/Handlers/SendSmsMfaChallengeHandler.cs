using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed class SendSmsMfaChallengeHandler(ISignInMfaService signInMfa) :
    ICommandHandler<SendSmsMfaChallengeCommand, SmsMfaChallengeResult>
{
    public Task<SmsMfaChallengeResult> Handle(SendSmsMfaChallengeCommand command, CancellationToken cancellationToken) =>
        signInMfa.SendSmsChallengeAsync(command.MfaToken, cancellationToken);
}
