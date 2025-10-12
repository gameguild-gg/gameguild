using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication;

public interface IWeb3Service
{
    Task<Web3ChallengeResponse> GenerateChallengeAsync(Web3ChallengeRequest request);

    Task<bool> VerifySignatureAsync(Web3AuthenticationVerificationRequest request);

    Task<User> FindOrCreateWeb3UserAsync(string walletAddress, string chainId);
}
