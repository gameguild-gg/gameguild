using GameGuild.Authentication.Models.Responses;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to verify Web3 signature and authenticate a user
/// </summary>
public class VerifyWeb3SignatureCommand : IRequest<SignInResponse>
{
    public string WalletAddress { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public string ChainId { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public string? DeviceFingerprint { get; set; }
}
