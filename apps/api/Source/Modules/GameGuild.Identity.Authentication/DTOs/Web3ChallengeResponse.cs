namespace GameGuild.Identity.Authentication;

/// <summary>
///     Response containing Web3 challenge data
/// </summary>
public class Web3ChallengeResponse
{
    public string Challenge { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
