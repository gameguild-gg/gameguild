using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Handles JWT generation and caching for the App Store Server API.
///     Uses App Store Connect API keys (ECDSA P-256) to create short-lived tokens.
/// </summary>
public class AppleStoreAuthService(
    IOptions<ApplePaySettings> settings,
    ILogger<AppleStoreAuthService> logger) : IAppleStoreAuthService
{
    private readonly ApplePaySettings _settings = settings.Value;
    private string? _cachedJwt;
    private DateTime _jwtExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _jwtLock = new(1, 1);

    /// <inheritdoc />
    public async Task<string?> GetAppStoreJwtAsync(CancellationToken cancellationToken = default)
    {
        // Check cached JWT
        if (!string.IsNullOrEmpty(_cachedJwt) && SystemClock.UtcNow < _jwtExpiresAt)
        {
            return _cachedJwt;
        }

        await _jwtLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedJwt) && SystemClock.UtcNow < _jwtExpiresAt)
            {
                return _cachedJwt;
            }

            // Load private key
            var privateKey = await LoadPrivateKeyAsync(cancellationToken).ConfigureAwait(false);
            if (privateKey == null)
            {
                logger.LogError("Failed to load App Store Connect API private key");
                return null;
            }

            // Generate JWT
            var now = SystemClock.UtcNow;
            var expiry = now.AddMinutes(15); // App Store Server API JWTs are valid for up to 60 minutes

            var securityKey = new ECDsaSecurityKey(privateKey) { KeyId = _settings.KeyId };
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

            var claims = new[]
            {
                new Claim("iss", _settings.TeamId),
                new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("exp", new DateTimeOffset(expiry).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("aud", "appstoreconnect-v1"),
                new Claim("bid", _settings.BundleId)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiry,
                SigningCredentials = credentials,
                Issuer = _settings.TeamId,
                Audience = "appstoreconnect-v1"
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            _cachedJwt = handler.WriteToken(token);
            _jwtExpiresAt = expiry.AddMinutes(-5); // Expire early to avoid edge cases

            return _cachedJwt;
        }
        finally
        {
            _jwtLock.Release();
        }
    }

    /// <summary>
    ///     Loads the App Store Connect API private key from content or file path.
    /// </summary>
    private async Task<ECDsa?> LoadPrivateKeyAsync(CancellationToken cancellationToken)
    {
        string? keyContent = _settings.PrivateKeyContent;

        if (string.IsNullOrEmpty(keyContent) && !string.IsNullOrEmpty(_settings.PrivateKeyPath))
        {
            if (!File.Exists(_settings.PrivateKeyPath))
            {
                logger.LogError("App Store Connect API private key file not found: {Path}", _settings.PrivateKeyPath);
                return null;
            }
            keyContent = await File.ReadAllTextAsync(_settings.PrivateKeyPath, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(keyContent))
        {
            return null;
        }

        // Parse the P8 key (PKCS#8 format)
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(keyContent);
        return ecdsa;
    }
}
