using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameGuild.Commerce.Billing;

/// <summary>Response from App Store Server API transaction lookup</summary>
public class AppleTransactionResponse
{
    [JsonPropertyName("signedTransactionInfo")]
    public string? SignedTransactionInfo { get; set; }

    [JsonPropertyName("signedRenewalInfo")]
    public string? SignedRenewalInfo { get; set; }
}

/// <summary>Decoded transaction info from Apple</summary>
public class AppleTransactionInfo
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
public class AppleNotificationPayload
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
public class AppleNotificationData
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
public class AppleJwsHeader
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
public partial class AppleJsonContext : JsonSerializerContext
{
}
