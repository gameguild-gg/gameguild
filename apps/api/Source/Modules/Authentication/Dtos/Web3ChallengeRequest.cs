namespace GameGuild.Modules.Authentication;

/// <summary>
/// Request DTO for Web3 authentication challenge generation
/// </summary>
public class Web3ChallengeRequest
{
    [Required]
    public string WalletAddress { get; set; } = string.Empty;

    public string? ChainId { get; set; }
}
