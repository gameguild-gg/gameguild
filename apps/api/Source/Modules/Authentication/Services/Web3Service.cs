using System.Security.Cryptography;
using GameGuild.Database;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Authentication {
  public class Web3Service(ApplicationDbContext context, ILogger<Web3Service> logger) : IWeb3Service {
    private readonly Dictionary<string, Web3ChallengeResponseDto> _challenges = new();

    public async Task<Web3ChallengeResponseDto> GenerateChallengeAsync(Web3ChallengeRequestDto request) {
      // Validate wallet address
      if (!IsValidEthereumAddress(request.WalletAddress)) throw new ArgumentException("Invalid Ethereum address", nameof(request.WalletAddress));

      var nonce = GenerateNonce();
      var challenge = GenerateChallenge(request.WalletAddress, nonce);
      var expiresAt = DateTime.UtcNow.AddMinutes(5); // 5-minute expiration

      var challengeResponse = new Web3ChallengeResponseDto { Challenge = challenge, Nonce = nonce, ExpiresAt = expiresAt };

      // Store challenge temporarily (in production, use Redis or database)
      _challenges[nonce] = challengeResponse;

      // Clean up expired challenges
      var expiredKeys = _challenges.Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();

      foreach (var key in expiredKeys) _challenges.Remove(key);

      return await Task.FromResult(challengeResponse);
    }

    public async Task<bool> VerifySignatureAsync(Web3VerifyRequestDto request) {
      // Check if challenge exists and is valid
      if (!_challenges.TryGetValue(request.Nonce, out var challenge)) {
        logger.LogWarning("Invalid or expired nonce: {Nonce}", request.Nonce);

        return false;
      }

      if (challenge.ExpiresAt < DateTime.UtcNow) {
        _challenges.Remove(request.Nonce);
        logger.LogWarning("Expired challenge for nonce: {Nonce}", request.Nonce);

        return false;
      }

      // Verify wallet address matches
      if (!request.WalletAddress.Equals(
            ExtractWalletFromChallenge(challenge.Challenge),
            StringComparison.OrdinalIgnoreCase
          )) {
        logger.LogWarning("Wallet address mismatch for nonce: {Nonce}", request.Nonce);

        return false;
      }

      // In a real implementation, you would verify the signature using a library like Nethereum
      // For now, we'll do a basic validation
      var isValidSignature =
        await VerifyEthereumSignature(challenge.Challenge, request.Signature, request.WalletAddress);

      if (isValidSignature)
        // Remove used challenge
        _challenges.Remove(request.Nonce);

      return isValidSignature;
    }

    public async Task<User> FindOrCreateWeb3UserAsync(string walletAddress, string chainId = "1") {
      // Try to find user by wallet address in credentials
      var credential = await context.Credentials.Include(c => c.User)
                                    .FirstOrDefaultAsync(c => c.Type == "web3_wallet" && c.Value == walletAddress.ToLower());

      if (credential?.User != null) return credential.User;

      // Create new user with wallet address
      var user = new User {
        Id = Guid.NewGuid(),
        Name = $"User_{walletAddress[..8]}...", // Use first 8 chars of wallet as username
        Email = $"{walletAddress.ToLower()}@web3.local", // Placeholder email
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      context.Users.Add(user);

      // Add Web3 credential
      var web3Credential = new Credential {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Type = "web3_wallet",
        Value = walletAddress.ToLower(),
        Metadata = System.Text.Json.JsonSerializer.Serialize(new { ChainId = chainId, WalletType = "ethereum" }),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      context.Credentials.Add(web3Credential);
      await context.SaveChangesAsync();

      return user;
    }

    private static string GenerateNonce() {
      using var rng = RandomNumberGenerator.Create();
      var bytes = new byte[32];
      rng.GetBytes(bytes);

      return Convert.ToHexString(bytes).ToLower();
    }

    private static string GenerateChallenge(string walletAddress, string nonce) {
      var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

      return
        $"Sign this message to authenticate with GameGuild CMS.\n\nWallet: {walletAddress}\nNonce: {nonce}\nTimestamp: {timestamp}";
    }

    private static string ExtractWalletFromChallenge(string challenge) {
      // Extract wallet address from challenge message
      var lines = challenge.Split('\n');
      var walletLine = lines.FirstOrDefault(l => l.StartsWith("Wallet: "));

      return walletLine?["Wallet: ".Length..] ?? "";
    }

    private Task<bool> VerifyEthereumSignature(string message, string signature, string walletAddress) {
      try {
        // This is a simplified implementation
        // In production, use a library like Nethereum for proper signature verification

        // Basic validation checks
        if (string.IsNullOrEmpty(signature) || signature.Length < 132) // 0x + 130 hex chars
          return Task.FromResult(false);

        if (!signature.StartsWith("0x")) return Task.FromResult(false);

        if (!IsValidEthereumAddress(walletAddress)) return Task.FromResult(false);

        // For now, return true if basic validation passes
        // TODO: Implement actual signature verification using Nethereum
        logger.LogInformation(
          "Web3 signature verification for wallet {WalletAddress} - basic validation passed",
          walletAddress
        );

        return Task.FromResult(true);
      }
      catch (Exception ex) {
        logger.LogError(ex, "Error verifying Web3 signature for wallet {WalletAddress}", walletAddress);

        return Task.FromResult(false);
      }
    }

    public bool IsValidEthereumAddress(string address) {
      if (string.IsNullOrEmpty(address) || !address.StartsWith("0x") || address.Length != 42) return false;

      // Check if it's a valid hex string
      return address[2..].All(c => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F');
    }
  }
}
