using System.Security.Cryptography;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Services;

/// <summary>
///     Service for Web3 authentication operations
/// </summary>
public class Web3Service(ILogger<Web3Service> logger) : IWeb3Service
{
    // TODO: In production, use Redis or distributed cache instead of in-memory dictionary
    private readonly Dictionary<string, Web3Challenge> _challenges = new Dictionary<string, Web3Challenge>();

    public async Task<Web3Challenge> GenerateChallengeAsync(string walletAddress, Guid? tenantId = null)
    {
        // Validate wallet address
        if (!IsValidWalletAddress(walletAddress)) { throw new ArgumentException("Invalid Ethereum address", nameof(walletAddress)); }

        var nonce = GenerateNonce();
        var message = GenerateChallengeMessage(walletAddress, nonce);
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(5); // 5-minute expiration

        var challenge = new Web3Challenge { Message = message, WalletAddress = walletAddress, Nonce = nonce, IssuedAt = issuedAt, ExpiresAt = expiresAt, TenantId = tenantId };

        // Store challenge temporarily
        // TODO: In production, use Redis with expiration
        _challenges[nonce] = challenge;

        // Clean up expired challenges
        CleanupExpiredChallenges();

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

        // Find the challenge by searching for the wallet address in stored challenges
        var challenge = _challenges.Values.FirstOrDefault(c => c.WalletAddress.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) && c.Message == originalMessage);

        if (challenge == null)
        {
            logger.LogWarning("Challenge not found for wallet {WalletAddress}", walletAddress);

            return false;
        }

        // Check if challenge is expired
        if (!challenge.IsValid)
        {
            _challenges.Remove(challenge.Nonce);
            logger.LogWarning("Expired challenge for wallet {WalletAddress}", walletAddress);

            return false;
        }

        // Verify the signature
        var isValidSignature = await VerifyEthereumSignature(signature, walletAddress);

        if (isValidSignature)
        {
            // Remove used challenge
            _challenges.Remove(challenge.Nonce);
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

    private void CleanupExpiredChallenges()
    {
        var expiredKeys = _challenges.Where(kvp => !kvp.Value.IsValid).Select(kvp => kvp.Key).ToList();

        foreach (var key in expiredKeys) { _challenges.Remove(key); }
    }

    private Task<bool> VerifyEthereumSignature(string signature, string walletAddress)
    {
        try
        {
            // This is a simplified implementation
            // In production, use a library like Nethereum for proper signature verification

            // Basic validation checks
            if (string.IsNullOrEmpty(signature) || signature.Length < 132) // 0x + 130 hex chars
            {
                return Task.FromResult(false);
            }

            if (!signature.StartsWith("0x", StringComparison.Ordinal)) { return Task.FromResult(false); }

            // TODO: Implement actual signature verification using Nethereum
            // For now, return true if basic validation passes
            logger.LogInformation("Web3 signature verification for wallet {WalletAddress} - basic validation passed", walletAddress);

            return Task.FromResult(true);
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Cryptographic error verifying Web3 signature for wallet {WalletAddress}", walletAddress);

            return Task.FromResult(false);
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Format error verifying Web3 signature for wallet {WalletAddress}", walletAddress);

            return Task.FromResult(false);
        }
    }
}
