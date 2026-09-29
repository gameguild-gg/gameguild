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
    /// <returns>Challenge message to be signed by the user's wallet</returns>
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress);

    /// <inheritdoc cref="GenerateChallengeAsync(string)" />
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId);

    /// <summary>
    ///     Generates an EIP-4361 challenge for a specific enabled chain.
    /// </summary>
    /// <param name="walletAddress">The wallet address requesting authentication</param>
    /// <param name="chainId">EIP-155 chain ID that must be enabled for SIWE authentication</param>
    /// <returns>Challenge message to be signed by the user's wallet</returns>
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, string chainId);

    /// <summary>
    ///     Generates an EIP-4361 challenge for an enabled chain and optional tenant context.
    /// </summary>
    /// <param name="walletAddress">The wallet address requesting authentication</param>
    /// <param name="tenantId">Tenant context for the challenge</param>
    /// <param name="chainId">EIP-155 chain ID that must be enabled for SIWE authentication</param>
    /// <returns>Challenge message to be signed by the user's wallet</returns>
    Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId, string chainId);

    /// <summary>
    ///     Verifies a Web3 signature against the original challenge.
    ///     Validates SIWE fields, the expected origin, expiration, nonce, chain, and EIP-191 signature.
    /// </summary>
    /// <param name="walletAddress">The wallet address that signed the message</param>
    /// <param name="signature">The cryptographic signature from the wallet</param>
    /// <param name="originalMessage">The original challenge message that was signed</param>
    /// <returns>True if signature is valid and not expired</returns>
    Task<bool> VerifySignatureAsync(string walletAddress, string signature, string originalMessage);

    /// <summary>
    ///     Verifies a Web3 signature against the original challenge and expected chain.
    /// </summary>
    /// <param name="walletAddress">The wallet address that signed the message</param>
    /// <param name="signature">The cryptographic signature from the wallet</param>
    /// <param name="originalMessage">The original challenge message that was signed</param>
    /// <param name="expectedChainId">Chain ID submitted by the client for this verification</param>
    /// <returns>True if signature is valid and not expired</returns>
    Task<bool> VerifySignatureAsync(string walletAddress, string signature, string originalMessage, string? expectedChainId);

    /// <summary>
    ///     Verifies a Web3 signature against the original challenge, expected chain, and nonce.
    /// </summary>
    /// <param name="walletAddress">The wallet address that signed the message</param>
    /// <param name="signature">The cryptographic signature from the wallet</param>
    /// <param name="originalMessage">The original challenge message that was signed</param>
    /// <param name="expectedChainId">Chain ID submitted by the client for this verification</param>
    /// <param name="expectedNonce">Nonce submitted by the client for this verification</param>
    /// <returns>True if signature is valid and not expired</returns>
    Task<bool> VerifySignatureAsync(
        string walletAddress,
        string signature,
        string originalMessage,
        string? expectedChainId,
        string? expectedNonce);

    /// <summary>
    ///     Validates that a wallet address is properly formatted and checksummed.
    /// </summary>
    /// <param name="walletAddress">The wallet address to validate</param>
    /// <returns>True if the wallet address is valid</returns>
    bool IsValidWalletAddress(string walletAddress);
}
