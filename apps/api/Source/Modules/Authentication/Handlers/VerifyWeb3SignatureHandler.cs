using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for verifying Web3 signature </summary>
public class VerifyWeb3SignatureHandler(IAuthService authService) : IRequestHandler<VerifyWeb3SignatureCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(VerifyWeb3SignatureCommand request, CancellationToken cancellationToken)
    {
        var verifyRequest = new Web3AuthenticationVerificationRequest { WalletAddress = request.WalletAddress, Signature = request.Signature, Nonce = request.Nonce, ChainId = request.ChainId };

        return await authService.VerifyWeb3SignatureAsync(verifyRequest);
    }
}
