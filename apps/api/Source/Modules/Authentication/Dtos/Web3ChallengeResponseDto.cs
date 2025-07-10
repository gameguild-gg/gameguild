namespace GameGuild.Modules.Auth;

/// <summary>
/// Response DTO for Web3 authentication challenge
/// </summary>
public class Web3ChallengeResponseDto {
  public string Challenge { get; set; } = string.Empty;

  public string Nonce { get; set; } = string.Empty;

  public DateTime ExpiresAt { get; set; }
}
