using MediatR;
using GameGuild.Modules.Auth.Dtos;

namespace GameGuild.Modules.Auth.Commands;

/// <summary>
/// Command to verify Web3 signature and authenticate user
/// </summary>
public class VerifyWeb3SignatureCommand : IRequest<SignInResponseDto>
{
    private string _walletAddress = string.Empty;

    private string _signature = string.Empty;

    private string _nonce = string.Empty;

    private string _chainId = string.Empty;

    public string WalletAddress
    {
        get => _walletAddress;
        set => _walletAddress = value;
    }

    public string Signature
    {
        get => _signature;
        set => _signature = value;
    }

    public string Nonce
    {
        get => _nonce;
        set => _nonce = value;
    }

    public string ChainId
    {
        get => _chainId;
        set => _chainId = value;
    }
}
