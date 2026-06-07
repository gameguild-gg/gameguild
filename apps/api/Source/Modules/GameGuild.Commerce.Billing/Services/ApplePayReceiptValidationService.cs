using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Facade for Apple Pay receipt validation using App Store Server API.
///     Delegates JWS verification to <see cref="IAppleJwsVerificationService"/>
///     and authentication to <see cref="IAppleStoreAuthService"/>.
/// </summary>
public class ApplePayReceiptValidationService(
    HttpClient httpClient,
    IOptions<ApplePaySettings> settings,
    IAppleStoreAuthService authService,
    IAppleJwsVerificationService jwsVerificationService,
    ILogger<ApplePayReceiptValidationService> logger) : IApplePayReceiptValidationService
{
    private readonly ApplePaySettings _settings = settings.Value;

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
                logger.LogWarning("Bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, bundleId);
                return AppleReceiptValidationResult.Failed("Bundle ID mismatch");
            }

            // Get JWT for App Store Server API
            var jwt = await authService.GetAppStoreJwtAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(jwt))
            {
                return AppleReceiptValidationResult.Failed("Failed to generate App Store Server API JWT");
            }

            // Look up transaction
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.BaseUrl}/inApps/v1/transactions/{transactionId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            logger.LogDebug("Validating Apple transaction. TransactionId={TransactionId}", transactionId);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
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
            var transactionInfo = jwsVerificationService.DecodeSignedTransaction(transactionResponse.SignedTransactionInfo);

            if (transactionInfo == null)
            {
                return AppleReceiptValidationResult.Failed("Failed to decode signed transaction");
            }

            // Verify the bundle ID in the transaction matches
            if (transactionInfo.BundleId != _settings.BundleId)
            {
                logger.LogWarning("Transaction bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, transactionInfo.BundleId);
                return AppleReceiptValidationResult.Failed("Bundle ID mismatch in transaction");
            }

            logger.LogInformation(
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
            logger.LogError(ex, "Error validating Apple receipt. TransactionId={TransactionId}", transactionId);
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
            var notification = jwsVerificationService.DecodeSignedNotification(signedPayload);

            if (notification == null)
            {
                return Task.FromResult(AppleNotificationVerificationResult.Failed("Failed to decode signed notification"));
            }

            // Verify the notification is for our app
            var data = notification.Data;
            if (data?.BundleId != _settings.BundleId)
            {
                logger.LogWarning("Notification bundle ID mismatch. Expected={Expected}, Received={Received}",
                    _settings.BundleId, data?.BundleId);
                return Task.FromResult(AppleNotificationVerificationResult.Failed("Bundle ID mismatch"));
            }

            // Decode the signed transaction info if present
            AppleTransactionInfo? transactionInfo = null;
            var signedTransactionInfo = data.SignedTransactionInfo;
            if (!string.IsNullOrEmpty(signedTransactionInfo))
            {
                transactionInfo = jwsVerificationService.DecodeSignedTransaction(signedTransactionInfo);
            }

            logger.LogInformation(
                "Apple notification verified. Type={NotificationType}, Subtype={Subtype}, TransactionId={TransactionId}",
                notification.NotificationType, notification.Subtype, transactionInfo?.TransactionId);

            DateTime? expirationDate = null;
            if (transactionInfo?.ExpiresDate is long expiresDate)
            {
                expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(expiresDate).UtcDateTime;
            }

            return Task.FromResult(AppleNotificationVerificationResult.Success(
                notification.NotificationType,
                notification.Subtype,
                transactionInfo?.TransactionId ?? string.Empty,
                transactionInfo?.OriginalTransactionId ?? string.Empty,
                transactionInfo?.ProductId ?? string.Empty,
                expirationDate,
                data.Environment ?? "unknown"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying Apple notification");
            return Task.FromResult(AppleNotificationVerificationResult.Failed(ex.Message));
        }
    }
}
