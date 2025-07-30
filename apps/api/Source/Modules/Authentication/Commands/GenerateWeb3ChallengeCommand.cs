using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to generate a Web3 challenge for wallet authentication
/// </summary>
public class GenerateWeb3ChallengeCommand : IRequest<Web3ChallengeResponseDto> {
  public string WalletAddress { get; set; } = string.Empty;

  public string ChainId { get; set; } = string.Empty;
}
