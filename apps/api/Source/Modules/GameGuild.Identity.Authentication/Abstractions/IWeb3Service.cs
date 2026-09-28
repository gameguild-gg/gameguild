namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for Web3/blockchain authentication operations.
///     Handles wallet signature verification, challenge generation, and blockchain interactions.
/// </summary>
public interface IWeb3Service
{
    /// <summary>
    ///     Generates an EIP-4361 Sign-In with Ethereum challenge for an allowed chain.
    /// </summary>
    /// <param name="walletAddress">The wallet address requesting authentication</param>
    /// <param name="tenantId">Optional tenant context</param>
    /// <param name="chainId">EIP-155 chain ID that must be enabled for SIWE authentication</param>
    /// <returns>Challenge message to be signed by the user's wallet</returns>
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId = null, string chainId = "1");

    /// <summary>
    ///     Verifies a Web3 signature against the original challenge.
    ///     Validates SIWE fields, the expected origin, expiration, nonce, chain, and EIP-191 signature.
    /// </summary>
    /// <param name="walletAddress">The wallet address that signed the message</param>
    /// <param name="signature">The cryptographic signature from the wallet</param>
    /// <param name="originalMessage">The original challenge message that was signed</param>
    /// <param name="expectedChainId">Optional chain ID submitted by the client for this verification</param>
    /// <param name="expectedNonce">Optional nonce submitted by the client for this verification</param>
    /// <returns>True if signature is valid and not expired</returns>
    Task<bool> VerifySignatureAsync(
        string walletAddress,
        string signature,
        string originalMessage,
        string? expectedChainId = null,
        string? expectedNonce = null);

    /// <summary>
    ///     Validates that a wallet address is properly formatted and checksummed.
    /// </summary>
    /// <param name="walletAddress">The wallet address to validate</param>
    /// <returns>True if the wallet address is valid</returns>
    bool IsValidWalletAddress(string walletAddress);
}
