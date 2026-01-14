using System.Security.Claims;
using System.Text.Encodings.Web;
using GameGuild.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authentication handler for API key-based authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApplicationDbContext _dbContext;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApplicationDbContext dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for API key in header
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Compute hash of provided key
            var keyHash = ComputeHash(providedApiKey);

            // Look up API key in database
            var apiKey = await _dbContext.Set<ApiKey>()
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

            if (apiKey == null)
            {
                Logger.LogWarning("Invalid API key provided");
                return AuthenticateResult.Fail("Invalid API key");
            }

            // Validate key
            if (!apiKey.IsValid())
            {
                Logger.LogWarning("Inactive or expired API key used: {KeyId}", apiKey.Id);
                return AuthenticateResult.Fail("API key is inactive or expired");
            }

            // Check IP whitelist if configured
            if (!string.IsNullOrWhiteSpace(apiKey.IpWhitelist))
            {
                var clientIp = Context.Connection.RemoteIpAddress?.ToString();
                var allowedIps = apiKey.IpWhitelist.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (clientIp == null || !allowedIps.Contains(clientIp))
                {
                    Logger.LogWarning("API key {KeyId} used from unauthorized IP: {ClientIp}", apiKey.Id, clientIp);
                    return AuthenticateResult.Fail("API key not authorized from this IP address");
                }
            }

            // Record usage
            apiKey.RecordUsage();
            await _dbContext.SaveChangesAsync(Context.RequestAborted);

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

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("Authentication error");
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
}

/// <summary>
///     Extension methods for API key authentication
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationOptions.SchemeName,
            options => { });
    }
}
