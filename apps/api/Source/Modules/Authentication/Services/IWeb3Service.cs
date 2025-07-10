using GameGuild.Modules.Users;


namespace GameGuild.Modules.Auth;

public interface IWeb3Service {
  Task<Web3ChallengeResponseDto> GenerateChallengeAsync(Web3ChallengeRequestDto request);

  Task<bool> VerifySignatureAsync(Web3VerifyRequestDto request);

  Task<User> FindOrCreateWeb3UserAsync(string walletAddress, string chainId);
}
