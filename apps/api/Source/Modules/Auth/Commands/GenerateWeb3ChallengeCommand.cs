using MediatR;
using GameGuild.Modules.Auth.Dtos;

namespace GameGuild.Modules.Auth.Commands;

/// <summary>
/// Command to generate Web3 challenge for wallet authentication
/// </summary>
public class GenerateWeb3ChallengeCommand : IRequest<Web3ChallengeResponseDto> {
  private string _walletAddress = string.Empty;

  private string _chainId = string.Empty;

  public string WalletAddress {
    get => _walletAddress;
    set => _walletAddress = value;
  }

  public string ChainId {
    get => _chainId;
    set => _chainId = value;
  }
}
