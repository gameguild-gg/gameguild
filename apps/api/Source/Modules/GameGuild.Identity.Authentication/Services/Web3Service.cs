using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nethereum.Signer;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for Web3 authentication operations
/// </summary>
public class Web3Service(ILogger<Web3Service> logger, IMemoryCache memoryCache, IDistributedCache? distributedCache = null) : IWeb3Service
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Cache key prefix for Web3 challenges.</summary>
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

        var cacheKey = ChallengeKeyPrefix + nonce;
        var walletKey = ChallengeKeyPrefix + "wallet:" + walletAddress.ToLowerInvariant();
        await StoreChallengeAsync(cacheKey, walletKey, challenge).ConfigureAwait(false);

        logger.LogInformation("Generated Web3 challenge for wallet {WalletAddress}", walletAddress);

        return challenge;
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
        var nonce = await GetNonceAsync(walletKey).ConfigureAwait(false);
        if (nonce == null)
        {
            logger.LogWarning("Challenge not found for wallet {WalletAddress}", walletAddress);

            return false;
        }

        var cacheKey = ChallengeKeyPrefix + nonce;
        var challenge = await GetChallengeAsync(cacheKey).ConfigureAwait(false);
        if (challenge == null)
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
            await RemoveChallengeAsync(cacheKey, walletKey).ConfigureAwait(false);
            logger.LogWarning("Expired challenge for wallet {WalletAddress}", walletAddress);

            return false;
        }

        // Verify the signature
        var isValidSignature = await VerifyEthereumSignature(signature, walletAddress, challenge.Message).ConfigureAwait(false);

        if (isValidSignature)
        {
            await RemoveChallengeAsync(cacheKey, walletKey).ConfigureAwait(false);
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

    private async Task StoreChallengeAsync(string cacheKey, string walletKey, Web3Challenge challenge)
    {
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = challenge.ExpiresAt,
            Size = 1
        };

        memoryCache.Set(cacheKey, challenge, memoryOptions);
        memoryCache.Set(walletKey, challenge.Nonce, memoryOptions);

        if (distributedCache == null)
        {
            return;
        }

        var distributedOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = challenge.ExpiresAt
        };

        await distributedCache.SetAsync(
            cacheKey,
            JsonSerializer.SerializeToUtf8Bytes(challenge, CacheJsonOptions),
            distributedOptions).ConfigureAwait(false);

        await distributedCache.SetAsync(
            walletKey,
            Encoding.UTF8.GetBytes(challenge.Nonce),
            distributedOptions).ConfigureAwait(false);
    }

    private async Task<string?> GetNonceAsync(string walletKey)
    {
        if (memoryCache.TryGetValue(walletKey, out string? nonce) && nonce != null)
        {
            return nonce;
        }

        if (distributedCache == null)
        {
            return null;
        }

        var bytes = await distributedCache.GetAsync(walletKey).ConfigureAwait(false);
        return bytes == null || bytes.Length == 0
            ? null
            : Encoding.UTF8.GetString(bytes);
    }

    private async Task<Web3Challenge?> GetChallengeAsync(string cacheKey)
    {
        if (memoryCache.TryGetValue(cacheKey, out Web3Challenge? challenge) && challenge != null)
        {
            return challenge;
        }

        if (distributedCache == null)
        {
            return null;
        }

        var bytes = await distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        challenge = JsonSerializer.Deserialize<Web3Challenge>(bytes, CacheJsonOptions);
        if (challenge == null)
        {
            return null;
        }

        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = challenge.ExpiresAt,
            Size = 1
        };
        memoryCache.Set(cacheKey, challenge, memoryOptions);
        memoryCache.Set(ChallengeKeyPrefix + "wallet:" + challenge.WalletAddress.ToLowerInvariant(), challenge.Nonce, memoryOptions);

        return challenge;
    }

    private async Task RemoveChallengeAsync(string cacheKey, string walletKey)
    {
        memoryCache.Remove(cacheKey);
        memoryCache.Remove(walletKey);

        if (distributedCache == null)
        {
            return;
        }

        await distributedCache.RemoveAsync(cacheKey).ConfigureAwait(false);
        await distributedCache.RemoveAsync(walletKey).ConfigureAwait(false);
    }


    private Task<bool> VerifyEthereumSignature(string signature, string walletAddress, string message)
    {
        // Basic format validation
        if (string.IsNullOrEmpty(signature) || signature.Length < 132) // 0x + 130 hex chars
        {
            return Task.FromResult(false);
        }

        if (!signature.StartsWith("0x", StringComparison.Ordinal)) { return Task.FromResult(false); }

        try
        {
            var signer = new EthereumMessageSigner();
            var recoveredAddress = signer.EncodeUTF8AndEcRecover(message, signature);
            var isMatch = string.Equals(recoveredAddress, walletAddress, StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                logger.LogWarning(
                    "Recovered Web3 signer {RecoveredAddress} does not match expected wallet {WalletAddress}",
                    recoveredAddress,
                    walletAddress);
            }

            return Task.FromResult(isMatch);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to recover Web3 signer for wallet {WalletAddress}", walletAddress);

            return Task.FromResult(false);
        }
    }
}
