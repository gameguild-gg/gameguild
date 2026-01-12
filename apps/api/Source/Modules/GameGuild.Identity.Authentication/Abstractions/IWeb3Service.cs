namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for Web3/blockchain authentication operations.
///     Handles wallet signature verification, challenge generation, and blockchain interactions.
/// </summary>
public interface IWeb3Service
{
    /// <summary>
    ///     Generates a unique challenge message for Web3 authentication.
    ///     Challenge includes timestamp, nonce, and application identifier to prevent replay attacks.
    /// </summary>
    /// <param name="walletAddress">The wallet address requesting authentication</param>
    /// <param name="tenantId">Optional tenant context</param>
    /// <returns>Challenge message to be signed by the user's wallet</returns>
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId = null);

    /// <summary>
    ///     Verifies a Web3 signature against the original challenge.
    ///     Validates signature authenticity, expiration, and that it matches the wallet address.
    /// </summary>
    /// <param name="walletAddress">The wallet address that signed the message</param>
    /// <param name="signature">The cryptographic signature from the wallet</param>
    /// <param name="originalMessage">The original challenge message that was signed</param>
    /// <returns>True if signature is valid and not expired</returns>
    Task<bool> VerifySignatureAsync(string walletAddress, string signature, string originalMessage);

    /// <summary>
    ///     Validates that a wallet address is properly formatted and checksummed.
    /// </summary>
    /// <param name="walletAddress">The wallet address to validate</param>
    /// <returns>True if the wallet address is valid</returns>
    bool IsValidWalletAddress(string walletAddress);
}
