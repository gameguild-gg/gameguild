using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Request DTO for Web3 authentication verification
/// </summary>
public class Web3VerifyRequestDto {
  [Required] public string WalletAddress { get; set; } = string.Empty;

  [Required] public string Signature { get; set; } = string.Empty;

  [Required] public string Nonce { get; set; } = string.Empty;

  public string? ChainId { get; set; }

  /// <summary>
  /// Optional tenant ID to use for the sign-in
  /// If not provided, will use the first available tenant for the user
  /// </summary>
  public Guid? TenantId { get; set; }
}
