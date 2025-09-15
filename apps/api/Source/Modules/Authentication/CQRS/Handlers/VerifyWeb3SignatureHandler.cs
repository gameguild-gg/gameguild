namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for verifying Web3 signature
/// </summary>
public class VerifyWeb3SignatureHandler(IAuthService authService) : IRequestHandler<VerifyWeb3SignatureCommand, SignInResponseDto> {
  public async Task<SignInResponseDto> Handle(VerifyWeb3SignatureCommand request, CancellationToken cancellationToken) {
    var verifyRequest = new Web3VerifyRequestDto { WalletAddress = request.WalletAddress, Signature = request.Signature, Nonce = request.Nonce, ChainId = request.ChainId };

    return await authService.VerifyWeb3SignatureAsync(verifyRequest);
  }
}
