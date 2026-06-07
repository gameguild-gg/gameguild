using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for Web3 authentication operations
/// </summary>
public class Web3Service(ILogger<Web3Service> logger, IMemoryCache memoryCache) : IWeb3Service
{
    /// <summary>Cache key prefix for Web3 challenges. Production should use IDistributedCache (Redis).</summary>
    private const string ChallengeKeyPrefix = "web3:challenge:";

    public async Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId = null)
    {
        // Validate wallet address
        if (!IsValidWalletAddress(walletAddress)) { throw new ArgumentException("Invalid Ethereum address", nameof(walletAddress)); }

        var nonce = GenerateNonce();
        var message = GenerateChallengeMessage(walletAddress, nonce);
        var issuedAt = SystemClock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(5); // 5-minute expiration

        var challenge = new Web3Challenge { Message = message, WalletAddress = walletAddress, Nonce = nonce, IssuedAt = issuedAt, ExpiresAt = expiresAt, TenantId = tenantId };

        // Store challenge in cache with automatic expiration
        var cacheKey = ChallengeKeyPrefix + nonce;
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };
        memoryCache.Set(cacheKey, challenge, cacheOptions);

        // Also store a wallet→nonce lookup for VerifySignatureAsync
        var walletKey = ChallengeKeyPrefix + "wallet:" + walletAddress.ToLowerInvariant();
        memoryCache.Set(walletKey, nonce, cacheOptions);

        logger.LogInformation("Generated Web3 challenge for wallet {WalletAddress}", walletAddress);

        return await Task.FromResult(challenge);
    }

    public async Task<bool> VerifySignatureAsync(string walletAddress, string signature, string originalMessage)
    {
        // Validate wallet address
        if (!IsValidWalletAddress(walletAddress))
        {
            logger.LogWarning("Invalid wallet address format: {WalletAddress}", walletAddress);

            return false;
        }

        // Find the challenge via wallet→nonce lookup
        var walletKey = ChallengeKeyPrefix + "wallet:" + walletAddress.ToLowerInvariant();
        if (!memoryCache.TryGetValue(walletKey, out string? nonce) || nonce == null)
        {
            logger.LogWarning("Challenge not found for wallet {WalletAddress}", walletAddress);

            return false;
        }

        var cacheKey = ChallengeKeyPrefix + nonce;
        if (!memoryCache.TryGetValue(cacheKey, out Web3Challenge? challenge) || challenge == null)
        {
            logger.LogWarning("Challenge not found for wallet {WalletAddress}", walletAddress);

            return false;
        }

        if (challenge.Message != originalMessage)
        {
            logger.LogWarning("Challenge message mismatch for wallet {WalletAddress}", walletAddress);

            return false;
        }

        // Check if challenge is expired
        if (!challenge.IsValid)
        {
            memoryCache.Remove(cacheKey);
            memoryCache.Remove(walletKey);
            logger.LogWarning("Expired challenge for wallet {WalletAddress}", walletAddress);

            return false;
        }

        // Verify the signature
        var isValidSignature = await VerifyEthereumSignature(signature, walletAddress).ConfigureAwait(false);

        if (isValidSignature)
        {
            // Remove used challenge from cache
            memoryCache.Remove(cacheKey);
            memoryCache.Remove(walletKey);
            logger.LogInformation("Successfully verified Web3 signature for wallet {WalletAddress}", walletAddress);
        }
        else { logger.LogWarning("Invalid signature for wallet {WalletAddress}", walletAddress); }

        return isValidSignature;
    }

    public bool IsValidWalletAddress(string walletAddress)
    {
        if (string.IsNullOrEmpty(walletAddress) || !walletAddress.StartsWith("0x", StringComparison.Ordinal) || walletAddress.Length != 42) { return false; }

        // Check if it's a valid hex string
        return walletAddress[2..].All(c => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F');
    }

    private static string GenerateNonce()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateChallengeMessage(string walletAddress, string nonce)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"Sign this message to authenticate with GameGuild.\n\n" + $"Wallet: {walletAddress}\n" + $"Nonce: {nonce}\n" + $"Timestamp: {timestamp}";
    }


    private Task<bool> VerifyEthereumSignature(string signature, string walletAddress)
    {
        // Basic format validation
        if (string.IsNullOrEmpty(signature) || signature.Length < 132) // 0x + 130 hex chars
        {
            return Task.FromResult(false);
        }

        if (!signature.StartsWith("0x", StringComparison.Ordinal)) { return Task.FromResult(false); }

        // SECURITY: Actual cryptographic signature verification is NOT implemented.
        // Add the Nethereum NuGet package and implement EcRecover to verify
        // that the signature was produced by the private key for walletAddress.
        // Until then, Web3 authentication MUST NOT be enabled in production.
        logger.LogError(
            "Web3 signature verification is not implemented — rejecting signature for wallet {WalletAddress}. " +
            "Add Nethereum package and implement EcRecover before enabling Web3 auth in production",
            walletAddress);

        return Task.FromResult(false);
    }
}
