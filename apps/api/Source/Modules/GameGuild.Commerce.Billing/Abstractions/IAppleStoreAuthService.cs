namespace GameGuild.Commerce.Billing;

/// <summary>
///     Service for authenticating with the App Store Server API.
///     Handles JWT generation and caching using App Store Connect API keys.
/// </summary>
public interface IAppleStoreAuthService
{
    /// <summary>
    ///     Gets a valid JWT for authenticating with the App Store Server API.
    ///     Returns a cached token when possible and generates a new one when expired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A valid JWT string, or null if key generation fails</returns>
    Task<string?> GetAppStoreJwtAsync(CancellationToken cancellationToken = default);
}
