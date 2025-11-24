using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Request for Web3 authentication verification
/// </summary>
public class Web3AuthenticationVerificationRequest
{
    [Required]
    public string WalletAddress { get; set; } = string.Empty;

    [Required]
    public string Signature { get; set; } = string.Empty;

    [Required]
    public string Nonce { get; set; } = string.Empty;

    public string ChainId { get; set; } = "1";
}
