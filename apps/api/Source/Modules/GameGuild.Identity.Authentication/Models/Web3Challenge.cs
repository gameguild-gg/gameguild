namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents a Web3 authentication challenge.
/// </summary>
public class Web3Challenge
{
    /// <summary>
    ///     The challenge message to be signed by the wallet.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     The wallet address requesting authentication.
    /// </summary>
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    ///     Unique nonce for this challenge (prevents replay attacks).
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    ///     When the challenge was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    ///     When the challenge expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     Optional tenant context for the challenge.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Gets whether the challenge is still valid (not expired).
    /// </summary>
    public bool IsValid { get => DateTime.UtcNow < ExpiresAt; }

    /// <summary>
    ///     Gets the number of seconds until expiration.
    /// </summary>
    public int SecondsUntilExpiration { get => Math.Max(0, (int) (ExpiresAt - DateTime.UtcNow).TotalSeconds); }
}
