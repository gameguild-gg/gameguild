using System.ComponentModel.DataAnnotations;

namespace GameGuild.Authentication.Models.Requests;

/// <summary>
///     Request for Web3 signature verification
/// </summary>
public class Web3VerificationRequest
{
    [Required(ErrorMessage = "Wallet address is required")]
    [MinLength(42, ErrorMessage = "Invalid wallet address")]
    [MaxLength(42, ErrorMessage = "Invalid wallet address")]
    public string WalletAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Signature is required")]
    [MaxLength(500)]
    public string Signature { get; set; } = string.Empty;

    [Required(ErrorMessage = "Challenge is required")]
    [MaxLength(500)]
    public string Challenge { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chain ID is required")]
    [MaxLength(10)]
    public string ChainId { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }
}
