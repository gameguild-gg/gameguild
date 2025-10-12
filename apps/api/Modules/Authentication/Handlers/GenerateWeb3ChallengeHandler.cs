using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for generating Web3 challenge </summary>
public class GenerateWeb3ChallengeHandler(IAuthService authService) : IRequestHandler<GenerateWeb3ChallengeCommand, Web3ChallengeResponse>
{
    public async Task<Web3ChallengeResponse> Handle(GenerateWeb3ChallengeCommand request, CancellationToken cancellationToken)
    {
        var challengeRequest = new Web3ChallengeRequest { WalletAddress = request.WalletAddress, ChainId = request.ChainId };

        return await authService.GenerateWeb3ChallengeAsync(challengeRequest);
    }
}
