using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Auth.Dtos {
  /// <summary>
  /// Request DTO for Web3 authentication challenge generation
  /// </summary>
  public class Web3ChallengeRequestDto {
    private string _walletAddress = string.Empty;

    private string? _chainId;

    [Required]
    public string WalletAddress {
      get => _walletAddress;
      set => _walletAddress = value;
    }

    public string? ChainId {
      get => _chainId;
      set => _chainId = value;
    }
  }

  /// <summary>
  /// Response DTO for Web3 authentication challenge
  /// </summary>
  public class Web3ChallengeResponseDto {
    private string _challenge = string.Empty;

    private string _nonce = string.Empty;

    private DateTime _expiresAt;

    public string Challenge {
      get => _challenge;
      set => _challenge = value;
    }

    public string Nonce {
      get => _nonce;
      set => _nonce = value;
    }

    public DateTime ExpiresAt {
      get => _expiresAt;
      set => _expiresAt = value;
    }
  }

  /// <summary>
  /// Request DTO for Web3 authentication verification
  /// </summary>
  public class Web3VerifyRequestDto {
    private string _walletAddress = string.Empty;

    private string _signature = string.Empty;

    private string _nonce = string.Empty;

    private string? _chainId;

    private Guid? _tenantId;

    [Required]
    public string WalletAddress {
      get => _walletAddress;
      set => _walletAddress = value;
    }

    [Required]
    public string Signature {
      get => _signature;
      set => _signature = value;
    }

    [Required]
    public string Nonce {
      get => _nonce;
      set => _nonce = value;
    }

    public string? ChainId {
      get => _chainId;
      set => _chainId = value;
    }

    /// <summary>
    /// Optional tenant ID to use for the sign-in
    /// If not provided, will use the first available tenant for the user
    /// </summary>
    public Guid? TenantId {
      get => _tenantId;
      set => _tenantId = value;
    }
  }
}
