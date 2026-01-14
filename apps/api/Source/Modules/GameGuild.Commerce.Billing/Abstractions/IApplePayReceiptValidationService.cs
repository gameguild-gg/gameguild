namespace GameGuild.Commerce.Billing;

/// <summary>
///     Service for validating Apple Pay receipts and transactions with the App Store.
/// </summary>
public interface IApplePayReceiptValidationService
{
    /// <summary>
    ///     Validates an Apple Pay receipt with the App Store Server API.
    /// </summary>
    /// <param name="receiptData">Base64-encoded receipt data</param>
    /// <param name="transactionId">Transaction identifier</param>
    /// <param name="bundleId">Expected bundle ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with transaction details</returns>
    Task<AppleReceiptValidationResult> ValidateReceiptAsync(
        string receiptData,
        string transactionId,
        string bundleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies an App Store Server Notification (webhook).
    /// </summary>
    /// <param name="signedPayload">The signedPayload from the notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result with decoded notification data</returns>
    Task<AppleNotificationVerificationResult> VerifyNotificationAsync(
        string signedPayload,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Result of Apple receipt validation.
/// </summary>
public record AppleReceiptValidationResult(
    bool IsValid,
    string? TransactionId,
    string? ProductId,
    DateTime? PurchaseDate,
    DateTime? ExpirationDate,
    string? Environment,
    string? ErrorMessage = null)
{
    /// <summary>Creates a success result</summary>
    public static AppleReceiptValidationResult Success(
        string transactionId,
        string productId,
        DateTime purchaseDate,
        DateTime? expirationDate,
        string environment) =>
        new(true, transactionId, productId, purchaseDate, expirationDate, environment);

    /// <summary>Creates a failure result</summary>
    public static AppleReceiptValidationResult Failed(string reason) =>
        new(false, null, null, null, null, null, reason);
}

/// <summary>
///     Result of Apple notification verification.
/// </summary>
public record AppleNotificationVerificationResult(
    bool IsValid,
    string? NotificationType,
    string? Subtype,
    string? TransactionId,
    string? OriginalTransactionId,
    string? ProductId,
    DateTime? ExpirationDate,
    string? Environment,
    string? ErrorMessage = null)
{
    /// <summary>Creates a success result</summary>
    public static AppleNotificationVerificationResult Success(
        string notificationType,
        string? subtype,
        string transactionId,
        string originalTransactionId,
        string productId,
        DateTime? expirationDate,
        string environment) =>
        new(true, notificationType, subtype, transactionId, originalTransactionId, productId, expirationDate, environment);

    /// <summary>Creates a failure result</summary>
    public static AppleNotificationVerificationResult Failed(string reason) =>
        new(false, null, null, null, null, null, null, null, reason);
}
