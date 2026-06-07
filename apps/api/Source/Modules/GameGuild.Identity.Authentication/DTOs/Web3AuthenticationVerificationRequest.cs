using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

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
    public string Challenge { get; set; } = string.Empty;
}

/// <summary>
/// Type alias for compatibility
/// </summary>
public class Web3VerificationRequest : Web3AuthenticationVerificationRequest
{
    [Required]
    public string Nonce { get; set; } = string.Empty;

    public string ChainId { get; set; } = "1";
}
