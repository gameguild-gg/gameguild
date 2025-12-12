namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Response for Web3 challenge
/// </summary>
public class Web3ChallengeResponse
{
    public string Challenge { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
