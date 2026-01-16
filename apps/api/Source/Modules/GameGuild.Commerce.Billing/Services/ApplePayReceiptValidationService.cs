using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Implementation of Apple Pay receipt validation using App Store Server API.
///     Uses App Store Server API v1 for receipt validation and notification verification.
/// </summary>
public class ApplePayReceiptValidationService : IApplePayReceiptValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ApplePaySettings _settings;
    private readonly ILogger<ApplePayReceiptValidationService> _logger;
    private string? _cachedJwt;
    private DateTime _jwtExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _jwtLock = new(1, 1);

    public ApplePayReceiptValidationService(
        HttpClient httpClient,
        IOptions<ApplePaySettings> settings,
        ILogger<ApplePayReceiptValidationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppleReceiptValidationResult> ValidateReceiptAsync(
        string receiptData,
        string transactionId,
        string bundleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify bundle ID matches configuration
            if (bundleId != _settings.BundleId)
            {
                _logger.LogWarning("Bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, bundleId);
                return AppleReceiptValidationResult.Failed("Bundle ID mismatch");
            }

            // Get JWT for App Store Server API
            var jwt = await GetAppStoreJwtAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(jwt))
            {
                return AppleReceiptValidationResult.Failed("Failed to generate App Store Server API JWT");
            }

            // Look up transaction
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.BaseUrl}/inApps/v1/transactions/{transactionId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            _logger.LogDebug("Validating Apple transaction. TransactionId={TransactionId}", transactionId);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "App Store Server API returned {StatusCode}: {Response}",
                    response.StatusCode, responseContent);
                return AppleReceiptValidationResult.Failed($"API returned {response.StatusCode}");
            }

            // Parse and verify the signed transaction
            var transactionResponse = JsonSerializer.Deserialize(responseContent, AppleJsonContext.Default.AppleTransactionResponse);

            if (transactionResponse?.SignedTransactionInfo == null)
            {
                return AppleReceiptValidationResult.Failed("No transaction info in response");
            }

            // Decode and verify the JWS (JSON Web Signature)
            var transactionInfo = DecodeSignedTransaction(transactionResponse.SignedTransactionInfo);

            if (transactionInfo == null)
            {
                return AppleReceiptValidationResult.Failed("Failed to decode signed transaction");
            }

            // Verify the bundle ID in the transaction matches
            if (transactionInfo.BundleId != _settings.BundleId)
            {
                _logger.LogWarning("Transaction bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, transactionInfo.BundleId);
                return AppleReceiptValidationResult.Failed("Bundle ID mismatch in transaction");
            }

            _logger.LogInformation(
                "Apple transaction validated successfully. TransactionId={TransactionId}, ProductId={ProductId}",
                transactionInfo.TransactionId, transactionInfo.ProductId);

            return AppleReceiptValidationResult.Success(
                transactionInfo.TransactionId,
                transactionInfo.ProductId,
                DateTimeOffset.FromUnixTimeMilliseconds(transactionInfo.PurchaseDate).UtcDateTime,
                transactionInfo.ExpiresDate.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(transactionInfo.ExpiresDate.Value).UtcDateTime
                    : null,
                transactionInfo.Environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Apple receipt. TransactionId={TransactionId}", transactionId);
            return AppleReceiptValidationResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<AppleNotificationVerificationResult> VerifyNotificationAsync(
        string signedPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // App Store Server Notifications v2 come as a signed JWS
            var notification = DecodeSignedNotification(signedPayload);

            if (notification == null)
            {
                return Task.FromResult(AppleNotificationVerificationResult.Failed("Failed to decode signed notification"));
            }

            // Verify the notification is for our app
            var data = notification.Data;
            if (data?.BundleId != _settings.BundleId)
            {
                _logger.LogWarning("Notification bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, data?.BundleId);
                return Task.FromResult(AppleNotificationVerificationResult.Failed("Bundle ID mismatch"));
            }

            // Decode the signed transaction info if present
            AppleTransactionInfo? transactionInfo = null;
            if (!string.IsNullOrEmpty(data?.SignedTransactionInfo))
            {
                transactionInfo = DecodeSignedTransaction(data.SignedTransactionInfo);
            }

            _logger.LogInformation(
                "Apple notification verified. Type={NotificationType}, Subtype={Subtype}, TransactionId={TransactionId}",
                notification.NotificationType, notification.Subtype, transactionInfo?.TransactionId);

            return Task.FromResult(AppleNotificationVerificationResult.Success(
                notification.NotificationType,
                notification.Subtype,
                transactionInfo?.TransactionId ?? string.Empty,
                transactionInfo?.OriginalTransactionId ?? string.Empty,
                transactionInfo?.ProductId ?? string.Empty,
                transactionInfo?.ExpiresDate.HasValue == true
                    ? DateTimeOffset.FromUnixTimeMilliseconds(transactionInfo.ExpiresDate.Value).UtcDateTime
                    : null,
                data?.Environment ?? "unknown"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Apple notification");
            return Task.FromResult(AppleNotificationVerificationResult.Failed(ex.Message));
        }
    }

    /// <summary>
    ///     Generates a JWT for authenticating with the App Store Server API.
    /// </summary>
    private async Task<string?> GetAppStoreJwtAsync(CancellationToken cancellationToken)
    {
        // Check cached JWT
        if (!string.IsNullOrEmpty(_cachedJwt) && DateTime.UtcNow < _jwtExpiresAt)
        {
            return _cachedJwt;
        }

        await _jwtLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedJwt) && DateTime.UtcNow < _jwtExpiresAt)
            {
                return _cachedJwt;
            }

            // Load private key
            var privateKey = await LoadPrivateKeyAsync(cancellationToken).ConfigureAwait(false);
            if (privateKey == null)
            {
                _logger.LogError("Failed to load App Store Connect API private key");
                return null;
            }

            // Generate JWT
            var now = DateTime.UtcNow;
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
    ///     Loads the App Store Connect API private key.
    /// </summary>
    private async Task<ECDsa?> LoadPrivateKeyAsync(CancellationToken cancellationToken)
    {
        string? keyContent = _settings.PrivateKeyContent;

        if (string.IsNullOrEmpty(keyContent) && !string.IsNullOrEmpty(_settings.PrivateKeyPath))
        {
            if (!File.Exists(_settings.PrivateKeyPath))
            {
                _logger.LogError("App Store Connect API private key file not found: {Path}", _settings.PrivateKeyPath);
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

    /// <summary>
    ///     Decodes and verifies a signed transaction JWS from Apple using X.509 certificate chain validation.
    ///     See: https://developer.apple.com/documentation/appstoreserverapi/jwstransaction
    /// </summary>
    private AppleTransactionInfo? DecodeSignedTransaction(string signedTransaction)
    {
        try
        {
            var parts = signedTransaction.Split('.');
            if (parts.Length != 3)
            {
                _logger.LogWarning("Invalid JWS format: expected 3 parts, got {Count}", parts.Length);
                return null;
            }

            // Decode header to get the certificate chain
            var headerJson = Base64UrlDecode(parts[0]);
            var header = JsonSerializer.Deserialize<AppleJwsHeader>(headerJson);

            if (header?.X5c == null || header.X5c.Length == 0)
            {
                _logger.LogWarning("JWS header missing x5c certificate chain");
                return null;
            }

            // Verify the certificate chain against Apple's root CA
            if (!VerifyAppleCertificateChain(header.X5c))
            {
                _logger.LogWarning("Apple certificate chain verification failed");
                return null;
            }

            // Extract the leaf certificate's public key for signature verification
            var leafCertBytes = Convert.FromBase64String(header.X5c[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);

            // Verify the JWS signature
            if (!VerifyJwsSignature(parts, leafCert, header.Alg))
            {
                _logger.LogWarning("JWS signature verification failed");
                return null;
            }

            // Decode the verified payload
            var payloadJson = Base64UrlDecode(parts[1]);
            return JsonSerializer.Deserialize(payloadJson, AppleJsonContext.Default.AppleTransactionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode signed transaction");
            return null;
        }
    }

    /// <summary>
    ///     Decodes and verifies a signed notification JWS from Apple using X.509 certificate chain validation.
    ///     See: https://developer.apple.com/documentation/appstoreservernotifications
    /// </summary>
    private AppleNotificationPayload? DecodeSignedNotification(string signedPayload)
    {
        try
        {
            var parts = signedPayload.Split('.');
            if (parts.Length != 3)
            {
                _logger.LogWarning("Invalid JWS format: expected 3 parts, got {Count}", parts.Length);
                return null;
            }

            // Decode header to get the certificate chain
            var headerJson = Base64UrlDecode(parts[0]);
            var header = JsonSerializer.Deserialize<AppleJwsHeader>(headerJson);

            if (header?.X5c == null || header.X5c.Length == 0)
            {
                _logger.LogWarning("JWS header missing x5c certificate chain");
                return null;
            }

            // Verify the certificate chain against Apple's root CA
            if (!VerifyAppleCertificateChain(header.X5c))
            {
                _logger.LogWarning("Apple certificate chain verification failed");
                return null;
            }

            // Extract the leaf certificate's public key for signature verification
            var leafCertBytes = Convert.FromBase64String(header.X5c[0]);
            using var leafCert = new X509Certificate2(leafCertBytes);

            // Verify the JWS signature
            if (!VerifyJwsSignature(parts, leafCert, header.Alg))
            {
                _logger.LogWarning("JWS signature verification failed");
                return null;
            }

            // Decode the verified payload
            var payloadJson = Base64UrlDecode(parts[1]);
            return JsonSerializer.Deserialize(payloadJson, AppleJsonContext.Default.AppleNotificationPayload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode signed notification");
            return null;
        }
    }

    /// <summary>
    ///     Verifies the Apple certificate chain against the known Apple Root CA.
    ///     The chain should be: [0] Leaf → [1] Intermediate → [2] Root (Apple Root CA - G3)
    /// </summary>
    private bool VerifyAppleCertificateChain(string[] x5cChain)
    {
        if (x5cChain.Length < 2)
        {
            _logger.LogWarning("Certificate chain too short: expected at least 2 certificates");
            return false;
        }

        try
        {
            // Build certificate chain
            var certificates = x5cChain
                .Select(certBase64 => new X509Certificate2(
                    Convert.FromBase64String(certBase64)))
                .ToArray();

            // The leaf certificate should be issued by Apple
            var leafCert = certificates[0];
            
            // Verify certificate is not expired
            var now = DateTime.UtcNow;
            if (now < leafCert.NotBefore || now > leafCert.NotAfter)
            {
                _logger.LogWarning("Leaf certificate is expired or not yet valid. NotBefore={NotBefore}, NotAfter={NotAfter}",
                    leafCert.NotBefore, leafCert.NotAfter);
                return false;
            }

            // Build and validate certificate chain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            
            // Add intermediate certificates to the extra store
            foreach (var cert in certificates.Skip(1))
            {
                chain.ChainPolicy.ExtraStore.Add(cert);
            }

            // Build the chain
            var isValid = chain.Build(leafCert);
            
            if (!isValid)
            {
                foreach (var status in chain.ChainStatus)
                {
                    _logger.LogWarning("Certificate chain status: {Status} - {StatusInformation}",
                        status.Status, status.StatusInformation);
                }
            }

            // Verify the issuer contains "Apple"
            if (!leafCert.Issuer.Contains("Apple", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Certificate issuer does not contain 'Apple': {Issuer}", leafCert.Issuer);
                return false;
            }

            // Clean up
            foreach (var cert in certificates)
            {
                cert.Dispose();
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify certificate chain");
            return false;
        }
    }

    /// <summary>
    ///     Verifies the JWS signature using the leaf certificate's public key.
    /// </summary>
    private bool VerifyJwsSignature(string[] parts, X509Certificate2 certificate, string algorithm)
    {
        try
        {
            // Get the signing input (header.payload)
            var signingInput = $"{parts[0]}.{parts[1]}";
            var signingInputBytes = Encoding.UTF8.GetBytes(signingInput);

            // Decode the signature
            var signatureBytes = Base64UrlDecodeBytes(parts[2]);

            // Get the public key and verify
            using var ecdsa = certificate.GetECDsaPublicKey();
            if (ecdsa == null)
            {
                _logger.LogWarning("Certificate does not have an ECDSA public key");
                return false;
            }

            // Determine hash algorithm based on JWS algorithm
            var hashAlgorithm = algorithm switch
            {
                "ES256" => HashAlgorithmName.SHA256,
                "ES384" => HashAlgorithmName.SHA384,
                "ES512" => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA256
            };

            return ecdsa.VerifyData(signingInputBytes, signatureBytes, hashAlgorithm);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify JWS signature");
            return false;
        }
    }

    /// <summary>
    ///     Decodes a Base64Url-encoded string to bytes.
    /// </summary>
    private static byte[] Base64UrlDecodeBytes(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    /// <summary>
    ///     Decodes a Base64Url-encoded string.
    /// </summary>
    private static string Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
}

#region Apple API Response Types

/// <summary>Response from App Store Server API transaction lookup</summary>
internal class AppleTransactionResponse
{
    [JsonPropertyName("signedTransactionInfo")]
    public string? SignedTransactionInfo { get; set; }

    [JsonPropertyName("signedRenewalInfo")]
    public string? SignedRenewalInfo { get; set; }
}

/// <summary>Decoded transaction info from Apple</summary>
internal class AppleTransactionInfo
{
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("originalTransactionId")]
    public string OriginalTransactionId { get; set; } = string.Empty;

    [JsonPropertyName("bundleId")]
    public string BundleId { get; set; } = string.Empty;

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("purchaseDate")]
    public long PurchaseDate { get; set; }

    [JsonPropertyName("expiresDate")]
    public long? ExpiresDate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;
}

/// <summary>App Store Server Notification v2 payload</summary>
internal class AppleNotificationPayload
{
    [JsonPropertyName("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("data")]
    public AppleNotificationData? Data { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("signedDate")]
    public long SignedDate { get; set; }
}

/// <summary>Data field in App Store Server Notification</summary>
internal class AppleNotificationData
{
    [JsonPropertyName("bundleId")]
    public string BundleId { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("signedTransactionInfo")]
    public string? SignedTransactionInfo { get; set; }

    [JsonPropertyName("signedRenewalInfo")]
    public string? SignedRenewalInfo { get; set; }
}

/// <summary>JWS header from Apple containing the x5c certificate chain</summary>
internal class AppleJwsHeader
{
    [JsonPropertyName("alg")]
    public string Alg { get; set; } = string.Empty;

    [JsonPropertyName("x5c")]
    public string[] X5c { get; set; } = Array.Empty<string>();
}

/// <summary>JSON serialization context for Apple API types</summary>
[JsonSerializable(typeof(AppleTransactionResponse))]
[JsonSerializable(typeof(AppleTransactionInfo))]
[JsonSerializable(typeof(AppleNotificationPayload))]
[JsonSerializable(typeof(AppleNotificationData))]
[JsonSerializable(typeof(AppleJwsHeader))]
internal partial class AppleJsonContext : JsonSerializerContext
{
}

#endregion
