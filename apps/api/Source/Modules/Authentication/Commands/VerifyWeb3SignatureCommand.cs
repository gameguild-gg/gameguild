using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to verify Web3 signature and authenticate a user
/// </summary>
public class VerifyWeb3SignatureCommand : IRequest<SignInResponseDto> {
  public string WalletAddress { get; set; } = string.Empty;

  public string Signature { get; set; } = string.Empty;

  public string Nonce { get; set; } = string.Empty;

  public string ChainId { get; set; } = string.Empty;
}
