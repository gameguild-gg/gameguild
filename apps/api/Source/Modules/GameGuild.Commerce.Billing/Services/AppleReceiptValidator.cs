using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Configuration options for Apple App Store Server Notifications.
/// </summary>
public class AppleAppStoreOptions
{
    public const string SectionName = "AppleAppStore";

    /// <summary>
    ///     Apple App Bundle ID
    /// </summary>
    public string BundleId { get; set; } = string.Empty;

    /// <summary>
    ///     Apple Team ID (from Apple Developer account)
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    ///     App Store Connect API Key ID
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    ///     App Store Connect API Private Key (PEM format)
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    ///     App Store Connect Issuer ID
    /// </summary>
    public string IssuerId { get; set; } = string.Empty;

    /// <summary>
    ///     Whether to use sandbox environment
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    ///     App Store Server API base URL
    /// </summary>
    public string BaseUrl => UseSandbox
        ? "https://api.storekit-sandbox.itunes.apple.com"
        : "https://api.storekit.itunes.apple.com";

    /// <summary>
    ///     Apple Root CA certificate URL for JWS verification
    /// </summary>
    public string AppleRootCaUrl { get; set; } = "https://www.apple.com/certificateauthority/AppleRootCA-G3.cer";
}

/// <summary>
///     Interface for Apple App Store receipt and notification validation.
/// </summary>
public interface IAppleReceiptValidator
{
    /// <summary>
    ///     Validates an App Store Server Notification V2 (JWS signed).
    /// </summary>
    /// <param name="signedPayload">The signed JWS payload from Apple</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with decoded notification data</returns>
    Task<AppleNotificationValidationResult> ValidateNotificationAsync(
        string signedPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets transaction history for a customer from App Store Server API.
    /// </summary>
    /// <param name="originalTransactionId">The original transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction history</returns>
    Task<AppleTransactionHistoryResult> GetTransactionHistoryAsync(
        string originalTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Requests a test notification from Apple to verify webhook configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test notification token</returns>
    Task<string?> RequestTestNotificationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of Apple notification validation.
/// </summary>
public class AppleNotificationValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NotificationType { get; set; }
    public string? Subtype { get; set; }
    public string? Environment { get; set; }
    public string? BundleId { get; set; }
    public string? OriginalTransactionId { get; set; }
    public string? TransactionId { get; set; }
    public string? ProductId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public AppleDecodedPayload? DecodedPayload { get; set; }

    public static AppleNotificationValidationResult Success(AppleDecodedPayload payload) => new()
    {
        IsValid = true,
        DecodedPayload = payload,
        NotificationType = payload.NotificationType,
        Subtype = payload.Subtype,
        Environment = payload.Environment,
        BundleId = payload.Data?.BundleId,
        OriginalTransactionId = payload.Data?.TransactionInfo?.OriginalTransactionId,
        TransactionId = payload.Data?.TransactionInfo?.TransactionId,
        ProductId = payload.Data?.TransactionInfo?.ProductId
    };

    public static AppleNotificationValidationResult Failed(string error) => new()
    {
        IsValid = false,
        ErrorMessage = error
    };
}

/// <summary>
///     Result of transaction history lookup.
/// </summary>
public class AppleTransactionHistoryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<AppleTransaction> Transactions { get; set; } = new();
    public string? Revision { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
///     Decoded Apple notification payload.
/// </summary>
public class AppleDecodedPayload
{
    [JsonPropertyName("notificationType")]
    public string? NotificationType { get; set; }

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("notificationUUID")]
    public string? NotificationUuid { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("signedDate")]
    public long? SignedDate { get; set; }

    [JsonPropertyName("data")]
    public AppleNotificationData? Data { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }
}

public class AppleNotificationData
{
    [JsonPropertyName("bundleId")]
    public string? BundleId { get; set; }

    [JsonPropertyName("bundleVersion")]
    public string? BundleVersion { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("signedTransactionInfo")]
    public string? SignedTransactionInfo { get; set; }

    [JsonPropertyName("signedRenewalInfo")]
    public string? SignedRenewalInfo { get; set; }

    // Decoded from signedTransactionInfo
    public AppleTransaction? TransactionInfo { get; set; }
}

public class AppleTransaction
{
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("originalTransactionId")]
    public string? OriginalTransactionId { get; set; }

    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }

    [JsonPropertyName("purchaseDate")]
    public long? PurchaseDate { get; set; }

    [JsonPropertyName("expiresDate")]
    public long? ExpiresDate { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("inAppOwnershipType")]
    public string? InAppOwnershipType { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("storefront")]
    public string? Storefront { get; set; }
}

/// <summary>
///     Apple App Store receipt and notification validator.
///     Implements App Store Server API V2 validation:
///     https://developer.apple.com/documentation/appstoreserverapi
/// </summary>
public class AppleReceiptValidator : IAppleReceiptValidator
{
    private readonly HttpClient _httpClient;
    private readonly AppleAppStoreOptions _options;
    private readonly ILogger<AppleReceiptValidator> _logger;
    private X509Certificate2? _appleRootCa;
    private readonly SemaphoreSlim _certLock = new(1, 1);

    public AppleReceiptValidator(
        HttpClient httpClient,
        IOptions<AppleAppStoreOptions> options,
        ILogger<AppleReceiptValidator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppleNotificationValidationResult> ValidateNotificationAsync(
        string signedPayload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(signedPayload))
            {
                return AppleNotificationValidationResult.Failed("Empty signed payload");
            }

            // App Store Server Notifications V2 are JWS (JSON Web Signature) format
            // They consist of three parts: header.payload.signature

            var parts = signedPayload.Split('.');
            if (parts.Length != 3)
            {
                return AppleNotificationValidationResult.Failed("Invalid JWS format - expected 3 parts");
            }

            // Decode and validate the JWS
            var headerJson = Base64UrlDecode(parts[0]);
            var payloadJson = Base64UrlDecode(parts[1]);

            var header = JsonSerializer.Deserialize<JwsHeader>(headerJson);
            if (header == null)
            {
                return AppleNotificationValidationResult.Failed("Failed to parse JWS header");
            }

            // Verify the certificate chain
            var certChainValid = await VerifyCertificateChainAsync(header.X5c, cancellationToken).ConfigureAwait(false);
            if (!certChainValid)
            {
                return AppleNotificationValidationResult.Failed("Certificate chain validation failed");
            }

            // Verify the signature using the leaf certificate
            var signatureValid = VerifySignature(parts[0], parts[1], parts[2], header.X5c, header.Alg);
            if (!signatureValid)
            {
                return AppleNotificationValidationResult.Failed("Signature verification failed");
            }

            // Decode the payload
            var decodedPayload = JsonSerializer.Deserialize<AppleDecodedPayload>(payloadJson);
            if (decodedPayload == null)
            {
                return AppleNotificationValidationResult.Failed("Failed to parse notification payload");
            }

            // Verify bundle ID matches
            if (decodedPayload.Data?.BundleId != _options.BundleId)
            {
                _logger.LogWarning("Bundle ID mismatch. Expected={Expected}, Got={Got}",
                    _options.BundleId, decodedPayload.Data?.BundleId);
                return AppleNotificationValidationResult.Failed("Bundle ID mismatch");
            }

            // Decode nested signed transaction info if present
            if (!string.IsNullOrEmpty(decodedPayload.Data?.SignedTransactionInfo))
            {
                decodedPayload.Data.TransactionInfo = DecodeSignedTransactionInfo(
                    decodedPayload.Data.SignedTransactionInfo);
            }

            _logger.LogInformation(
                "Apple notification validated. Type={Type}, TransactionId={TransactionId}",
                decodedPayload.NotificationType,
                decodedPayload.Data?.TransactionInfo?.TransactionId);

            return AppleNotificationValidationResult.Success(decodedPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Apple notification validation");
            return AppleNotificationValidationResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<AppleTransactionHistoryResult> GetTransactionHistoryAsync(
        string originalTransactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jwt = GenerateAppStoreConnectJwt();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_options.BaseUrl}/inApps/v1/history/{originalTransactionId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return new AppleTransactionHistoryResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"API error: {response.StatusCode} - {error}"
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var historyResponse = JsonSerializer.Deserialize<AppleHistoryResponse>(content);

            var transactions = new List<AppleTransaction>();
            if (historyResponse?.SignedTransactions != null)
            {
                foreach (var signed in historyResponse.SignedTransactions)
                {
                    var transaction = DecodeSignedTransactionInfo(signed);
                    if (transaction != null)
                    {
                        transactions.Add(transaction);
                    }
                }
            }

            return new AppleTransactionHistoryResult
            {
                IsSuccess = true,
                Transactions = transactions,
                Revision = historyResponse?.Revision,
                HasMore = historyResponse?.HasMore ?? false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Apple transaction history. TransactionId={TransactionId}",
                originalTransactionId);
            return new AppleTransactionHistoryResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<string?> RequestTestNotificationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var jwt = GenerateAppStoreConnectJwt();

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_options.BaseUrl}/inApps/v1/notifications/test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to request Apple test notification: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadFromJsonAsync<AppleTestNotificationResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return content?.TestNotificationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception requesting Apple test notification");
            return null;
        }
    }

    /// <summary>
    ///     Verifies the certificate chain against Apple's root CA.
    /// </summary>
    private async Task<bool> VerifyCertificateChainAsync(string[]? x5c, CancellationToken cancellationToken)
    {
        if (x5c == null || x5c.Length == 0)
        {
            return false;
        }

        try
        {
            // Load Apple Root CA if not cached
            var rootCa = await GetAppleRootCaAsync(cancellationToken).ConfigureAwait(false);
            if (rootCa == null)
            {
                _logger.LogError("Failed to load Apple Root CA certificate");
                return false;
            }

            // Build certificate chain
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.ExtraStore.Add(rootCa);

            // The first certificate in x5c is the leaf certificate
            var leafCertBytes = Convert.FromBase64String(x5c[0]);
            var leafCert = new X509Certificate2(leafCertBytes);

            // Add intermediate certificates
            for (int i = 1; i < x5c.Length; i++)
            {
                var intermediateCertBytes = Convert.FromBase64String(x5c[i]);
                chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(intermediateCertBytes));
            }

            var isValid = chain.Build(leafCert);

            if (!isValid)
            {
                foreach (var status in chain.ChainStatus)
                {
                    _logger.LogWarning("Certificate chain status: {Status} - {Info}",
                        status.Status, status.StatusInformation);
                }
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception verifying certificate chain");
            return false;
        }
    }

    /// <summary>
    ///     Verifies the JWS signature.
    /// </summary>
    private bool VerifySignature(string header, string payload, string signature, string[]? x5c, string? algorithm)
    {
        if (x5c == null || x5c.Length == 0 || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        try
        {
            var leafCertBytes = Convert.FromBase64String(x5c[0]);
            var leafCert = new X509Certificate2(leafCertBytes);

            var signingInput = $"{header}.{payload}";
            var signatureBytes = Base64UrlDecodeBytes(signature);

            using var ecdsa = leafCert.GetECDsaPublicKey();
            if (ecdsa == null)
            {
                _logger.LogError("Failed to get ECDSA public key from certificate");
                return false;
            }

            var dataBytes = Encoding.UTF8.GetBytes(signingInput);
            var hashAlgorithm = algorithm == "ES256" ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA384;

            return ecdsa.VerifyData(dataBytes, signatureBytes, hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception verifying JWS signature");
            return false;
        }
    }

    /// <summary>
    ///     Decodes a signed transaction info JWS.
    /// </summary>
    private AppleTransaction? DecodeSignedTransactionInfo(string signedInfo)
    {
        try
        {
            var parts = signedInfo.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payloadJson = Base64UrlDecode(parts[1]);
            return JsonSerializer.Deserialize<AppleTransaction>(payloadJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets Apple Root CA certificate.
    /// </summary>
    private async Task<X509Certificate2?> GetAppleRootCaAsync(CancellationToken cancellationToken)
    {
        await _certLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_appleRootCa != null)
            {
                return _appleRootCa;
            }

            var response = await _httpClient.GetByteArrayAsync(_options.AppleRootCaUrl, cancellationToken)
                .ConfigureAwait(false);
            _appleRootCa = new X509Certificate2(response);
            return _appleRootCa;
        }
        finally
        {
            _certLock.Release();
        }
    }

    /// <summary>
    ///     Generates a JWT for App Store Connect API authentication.
    /// </summary>
    private string GenerateAppStoreConnectJwt()
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(20); // Apple recommends 20 minute max

        var header = new
        {
            alg = "ES256",
            kid = _options.KeyId,
            typ = "JWT"
        };

        var payload = new
        {
            iss = _options.IssuerId,
            iat = now.ToUnixTimeSeconds(),
            exp = expiry.ToUnixTimeSeconds(),
            aud = "appstoreconnect-v1",
            bid = _options.BundleId
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var signingInput = $"{headerBase64}.{payloadBase64}";

        // Sign with private key
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_options.PrivateKey);

        var signatureBytes = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256);

        var signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
    }

    private static string Base64UrlDecode(string input)
    {
        var bytes = Base64UrlDecodeBytes(input);
        return Encoding.UTF8.GetString(bytes);
    }

    private static byte[] Base64UrlDecodeBytes(string input)
    {
        var output = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }

        return Convert.FromBase64String(output);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private class JwsHeader
    {
        [JsonPropertyName("alg")]
        public string? Alg { get; set; }

        [JsonPropertyName("x5c")]
        public string[]? X5c { get; set; }
    }

    private class AppleHistoryResponse
    {
        [JsonPropertyName("signedTransactions")]
        public string[]? SignedTransactions { get; set; }

        [JsonPropertyName("revision")]
        public string? Revision { get; set; }

        [JsonPropertyName("hasMore")]
        public bool HasMore { get; set; }
    }

    private class AppleTestNotificationResponse
    {
        [JsonPropertyName("testNotificationToken")]
        public string? TestNotificationToken { get; set; }
    }
}
