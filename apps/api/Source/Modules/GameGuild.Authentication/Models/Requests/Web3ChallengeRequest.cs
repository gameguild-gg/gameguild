using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for Web3 challenge generation
/// </summary>
public abstract class Web3ChallengeRequest
{
    [Required(ErrorMessage = "Wallet address is required")]
    [MinLength(42, ErrorMessage = "Invalid wallet address")]
    [MaxLength(42, ErrorMessage = "Invalid wallet address")]
    public string WalletAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chain ID is required")]
    [MaxLength(10)]
    public string ChainId { get; set; } = string.Empty;
}
