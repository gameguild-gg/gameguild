using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for Web3 challenge generation
/// </summary>
public class Web3ChallengeRequest
{
    [Required]
    public string WalletAddress { get; set; } = string.Empty;

    public string ChainId { get; set; } = "1"; // Default to Ethereum mainnet
}
