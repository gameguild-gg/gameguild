using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authentication handler for API key-based authentication
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    /// <summary>Issue #1192: failed validations (unknown key hashes) tolerated per source before lockout.</summary>
    public const int LockoutThreshold = 10;

    /// <summary>Issue #1192: how long a source stays locked after the threshold is reached.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    private const string LockoutKeyPrefix = "auth:apikey:lockout:";

    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IAuthenticationAuditEventSink? _auditEventSink;
    private readonly IDistributedCache? _distributedCache;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeyRepository)
        : this(options, logger, encoder, apiKeyRepository, null)
    {
    }

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeyRepository,
        IAuthenticationAuditEventSink? auditEventSink)
        : this(options, logger, encoder, apiKeyRepository, auditEventSink, null)
    {
    }

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeyRepository,
        IAuthenticationAuditEventSink? auditEventSink,
        IDistributedCache? distributedCache)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
        _auditEventSink = auditEventSink;
        _distributedCache = distributedCache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Entry-check contract (issue #1122): this method is the ONLY place a request's API
        // key is validated. The authentication middleware runs it once per request and the
        // framework caches the result for the request's lifetime (handlers are scoped per
        // request), so a key that expires — or is revoked/rotated out — while a long-running
        // handler is executing can never flip that in-flight request to 401: the principal,
        // scopes and key id below were frozen at entry. Expiry only blocks NEW requests,
        // which reach this method with the wall clock already past the key's window. Any
        // future mid-request authorization (e.g. ApiKeyScopeHandler) must consume the claims
        // minted here, never re-read the credential store.
        var headerApiKey = Request.Headers[Options.HeaderName].FirstOrDefault();
        var queryApiKey = Options.AllowQueryString
            ? Request.Query[Options.QueryStringParameterName].FirstOrDefault()
            : null;
        string? customApiKey;
        try
        {
            customApiKey = Options.CustomKeyResolver?.Invoke(Request);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reading custom API key source");
            return await FailAsync("Authentication error", "AuthenticationError").ConfigureAwait(false);
        }

        var configuredSources = new[] { headerApiKey, queryApiKey, customApiKey }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (configuredSources.Length > 1)
        {
            return await FailAsync(
                "Provide an API key through only one configured source.",
                "MultipleApiKeySources").ConfigureAwait(false);
        }

        var providedApiKey = configuredSources.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        if (!string.IsNullOrWhiteSpace(queryApiKey) && !Request.IsHttps)
        {
            return await FailAsync("API keys in query strings require HTTPS.", "ApiKeyQueryRequiresHttps").ConfigureAwait(false);
        }

        // Issue #1192: brute-force lockout on key validation. Unknown-hash failures are
        // counted per source address; past the threshold the source is locked for the window
        // and further attempts fail before touching the credential store. Mirrors the service
        // account lockout posture (10 failures / 30 minutes).
        if (await IsSourceLockedOutAsync().ConfigureAwait(false))
        {
            Logger.LogWarning("API key authentication rejected: source is locked out after repeated invalid keys");
            return await FailAsync("Too many invalid API key attempts; try again later", "ApiKeySourceLockedOut").ConfigureAwait(false);
        }

        ApiKey? apiKey = null;
        try
        {
            // Compute hash of provided key
            var keyHash = ComputeHash(providedApiKey);

            // Look up API key through the credential repository
            apiKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash, Context.RequestAborted).ConfigureAwait(false);

            if (apiKey == null)
            {
                Logger.LogWarning("Invalid API key provided");
                await RecordValidationFailureForSourceAsync().ConfigureAwait(false);
                return await FailAsync("Invalid API key", "InvalidApiKey").ConfigureAwait(false);
            }

            // A valid credential resets the source's failure budget (mirrors the service
            // account counter, which clears on success).
            await ClearValidationFailuresForSourceAsync().ConfigureAwait(false);

            // Validate key
            if (!apiKey.IsValid())
            {
                // Preserve lazy finalization as a fallback if scheduled maintenance is delayed.
                // Expired overlap windows are denied independently of either persistence path.
                await _apiKeyRepository.FinalizeRotationRevocationAsync(apiKey, Context.RequestAborted).ConfigureAwait(false);

                Logger.LogWarning("Inactive or expired API key used: {KeyId}", apiKey.Id);
                return await FailAsync("API key is inactive or expired", "InactiveOrExpiredApiKey", apiKey.UserId, apiKey.TenantId).ConfigureAwait(false);
            }

            // Check IP whitelist if configured
            if (!string.IsNullOrWhiteSpace(apiKey.IpWhitelist))
            {
                // Issue #1192: compare after normalizing IPv6-mapped IPv4 form on both sides —
                // Kestrel reports "::ffff:1.2.3.4" for IPv4 callers on dual-stack sockets, which
                // an exact-string compare against a "1.2.3.4" whitelist entry silently denies.
                var clientIp = NormalizeAddress(Context.Connection.RemoteIpAddress);
                var allowed = apiKey.IpWhitelist
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => NormalizeWhitelistEntry(entry.Trim()))
                    .ToList();
                if (clientIp is null || !allowed.Contains(clientIp))
                {
                    Logger.LogWarning("API key {KeyId} used from unauthorized IP: {ClientIp}", apiKey.Id, clientIp);
                    return await FailAsync("API key not authorized from this IP address", "ApiKeyIpNotAllowed", apiKey.UserId, apiKey.TenantId).ConfigureAwait(false);
                }
            }

            // Record usage
            await _apiKeyRepository.RecordUsageAsync(apiKey, Context.RequestAborted).ConfigureAwait(false);

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()),
                new("sub", apiKey.UserId.ToString()),
                new("tenant_id", apiKey.TenantId?.ToString() ?? string.Empty),
                new("api_key_id", apiKey.Id.ToString()),
                new("auth_method", "api_key")
            };

            // Add scope claims
            foreach (var scope in apiKey.GetScopes())
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("API key authentication successful for user {UserId}, key {KeyId}",
                apiKey.UserId, apiKey.Id);

            await RecordAuthenticationAuditEventAsync(success: true, apiKey.UserId, apiKey.TenantId).ConfigureAwait(false);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API key authentication");
            return await FailAsync("Authentication error", "AuthenticationError", apiKey?.UserId, apiKey?.TenantId).ConfigureAwait(false);
        }
    }

    private async Task<AuthenticateResult> FailAsync(string message, string auditReason, Guid? userId = null, Guid? tenantId = null)
    {
        await RecordAuthenticationAuditEventAsync(false, userId, tenantId, auditReason).ConfigureAwait(false);
        return AuthenticateResult.Fail(message);
    }

    /// <summary>
    ///     Issue #1192: whether this request's source address is currently locked out after
    ///     repeated unknown-key attempts. No-op (never locked) without a distributed cache.
    /// </summary>
    private async Task<bool> IsSourceLockedOutAsync()
    {
        if (_distributedCache is null)
        {
            return false;
        }

        var lockKey = SourceLockKey();
        if (lockKey is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(
            await _distributedCache.GetStringAsync(lockKey, Context.RequestAborted).ConfigureAwait(false));
    }

    /// <summary>
    ///     Issue #1192: counts one unknown-hash validation failure for the source address and
    ///     locks the source once the threshold is reached. Shared across instances through
    ///     <see cref="IDistributedCache"/>.
    /// </summary>
    private async Task RecordValidationFailureForSourceAsync()
    {
        if (_distributedCache is null)
        {
            return;
        }

        var lockKey = SourceLockKey();
        if (lockKey is null)
        {
            return;
        }

        try
        {
            var counterKey = lockKey + ":count";
            var raw = await _distributedCache.GetStringAsync(counterKey, Context.RequestAborted).ConfigureAwait(false);
            var failures = int.TryParse(raw, out var parsed) ? parsed : 0;
            failures++;
            if (failures >= LockoutThreshold)
            {
                await _distributedCache.SetStringAsync(
                    lockKey,
                    SystemClock.UtcNow.ToString("O"),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockoutDuration },
                    Context.RequestAborted).ConfigureAwait(false);
                await _distributedCache.RemoveAsync(counterKey, Context.RequestAborted).ConfigureAwait(false);
                Logger.LogWarning(
                    "API key source locked out for {Duration} after {Failures} invalid key attempts",
                    LockoutDuration, failures);
            }
            else
            {
                await _distributedCache.SetStringAsync(
                    counterKey,
                    failures.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockoutDuration },
                    Context.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Lockout accounting is hardening, not the authentication contract: a cache outage
            // must not turn every key validation into a failure.
            Logger.LogError(exception, "Could not record API key validation failure for lockout accounting");
        }
    }

    private async Task ClearValidationFailuresForSourceAsync()
    {
        if (_distributedCache is null)
        {
            return;
        }

        var lockKey = SourceLockKey();
        if (lockKey is null)
        {
            return;
        }

        try
        {
            await _distributedCache.RemoveAsync(lockKey + ":count", Context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not clear API key validation failure counter");
        }
    }

    private string? SourceLockKey()
    {
        var remote = Context.Connection.RemoteIpAddress;
        if (remote is null)
        {
            return null;
        }

        return LockoutKeyPrefix + (remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote);
    }

    /// <summary>
    ///     Issue #1192: normalizes an address (either side of the whitelist compare) out of
    ///     IPv6-mapped IPv4 form so <c>::ffff:1.2.3.4</c> and <c>1.2.3.4</c> compare equal.
    /// </summary>
    internal static string? NormalizeAddress(System.Net.IPAddress? address) =>
        address is null
            ? null
            : (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();

    internal static string NormalizeWhitelistEntry(string entry) =>
        System.Net.IPAddress.TryParse(entry, out var parsed)
            ? NormalizeAddress(parsed) ?? entry
            : entry;

    private async Task RecordAuthenticationAuditEventAsync(
        bool success,
        Guid? userId,
        Guid? tenantId,
        string? auditReason = null)
    {
        if (_auditEventSink is null)
        {
            return;
        }

        try
        {
            await _auditEventSink.RecordAsync(new AuthenticationAuditEvent(
                success ? "Authentication.Succeeded" : "Authentication.Failed",
                userId,
                success,
                "ApiKey",
                Context.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                TenantId: tenantId,
                ErrorMessage: auditReason),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not record API key authentication audit event");
        }
    }

    private static string ComputeHash(string plaintext)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
///     Options for API key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";

    public string HeaderName { get; set; } = "X-API-Key";

    public bool AllowQueryString { get; set; }

    public string QueryStringParameterName { get; set; } = "api_key";

    /// <summary>
    ///     Optional programmatic source for deployments that retrieve API keys from a custom request location.
    /// </summary>
    public Func<HttpRequest, string?>? CustomKeyResolver { get; set; }

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(HeaderName))
        {
            throw new InvalidOperationException("API key header name must not be empty.");
        }

        if (AllowQueryString && string.IsNullOrWhiteSpace(QueryStringParameterName))
        {
            throw new InvalidOperationException("API key query parameter name must not be empty when query authentication is enabled.");
        }
    }
}

/// <summary>
///     Extension methods for API key authentication
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder) =>
        AddApiKeyAuthentication(builder, configure: null);

    public static AuthenticationBuilder AddApiKeyAuthentication(
        this AuthenticationBuilder builder,
        Action<ApiKeyAuthenticationOptions>? configure)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationOptions.SchemeName,
            options =>
            {
                configure?.Invoke(options);
                options.Validate();
            });
    }
}
