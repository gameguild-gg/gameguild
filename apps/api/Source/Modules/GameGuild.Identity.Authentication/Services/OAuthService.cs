using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for OAuth authentication with various providers
/// </summary>
public class OAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<OAuthService> logger) : IOAuthService
{
    // OAuth endpoint constants
    private const string GitHubAuthUrl = "https://github.com/login/oauth/authorize";

    private const string GitHubTokenUrl = "https://github.com/login/oauth/access_token";

    private const string GitHubUserUrl = "https://api.github.com/user";

    private const string GitHubEmailUrl = "https://api.github.com/user/emails";

    private const string GoogleAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";

    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";

    private const string GoogleUserUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true };

    public Task<string> GetAuthorizationUrlAsync(string provider, string redirectUri, string state, string[ ]? scopes = null)
    {
        var clientId = configuration[$"OAuth:{provider}:ClientId"];

        if (string.IsNullOrEmpty(clientId)) { throw new InvalidOperationException($"OAuth client ID not configured for provider: {provider}"); }

        var url = provider.ToLower(CultureInfo.InvariantCulture) switch
        {
            "github" => BuildGitHubAuthUrl(clientId, redirectUri, state, scopes),
            "google" => BuildGoogleAuthUrl(clientId, redirectUri, state, scopes),
            _ => throw new NotSupportedException($"OAuth provider not supported: {provider}")
        };

        return Task.FromResult(url);
    }

    public async Task<OAuthUserProfile> HandleCallbackAsync(string provider, string code, string state, string redirectUri)
    {
        // Validate state parameter for CSRF protection
        if (string.IsNullOrEmpty(state))
        {
            logger.LogWarning("OAuth callback received without state parameter — potential CSRF attack for provider {Provider}", provider);
            throw new InvalidOperationException("Missing OAuth state parameter. Request may have been tampered with.");
        }

        var accessToken = await ExchangeCodeForTokenAsync(provider, code, redirectUri).ConfigureAwait(false);

        return await GetUserProfileAsync(provider, accessToken).ConfigureAwait(false);
    }

    public async Task<OAuthUserProfile> GetUserProfileAsync(string provider, string accessToken)
    {
        return provider.ToLower(CultureInfo.InvariantCulture) switch
        {
            "github" => await GetGitHubUserProfileAsync(accessToken).ConfigureAwait(false),
            "google" => await GetGoogleUserProfileAsync(accessToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Provider not supported: {provider}")
        };
    }

    public async Task<bool> RevokeTokenAsync(string provider, string token)
    {
        try
        {
            // Implementation varies by provider
            // For now, return true as most providers don't require explicit revocation
            logger.LogInformation("Token revocation requested for provider {Provider}", provider);
            await Task.CompletedTask.ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke token for provider {Provider}", provider);

            return false;
        }
    }

    #region Private Helpers

    private async Task<string> ExchangeCodeForTokenAsync(string provider, string code, string redirectUri)
    {
        return provider.ToLower(CultureInfo.InvariantCulture) switch
        {
            "github" => await ExchangeGitHubCodeAsync(code, redirectUri).ConfigureAwait(false),
            "google" => await ExchangeGoogleCodeAsync(code, redirectUri).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Provider not supported: {provider}")
        };
    }

    #endregion

    #region GitHub OAuth

    private string BuildGitHubAuthUrl(string clientId, string redirectUri, string state, string[ ]? scopes)
    {
        var scopeString = scopes != null && scopes.Length > 0 ? string.Join(" ", scopes) : "read:user user:email";

        return $"{GitHubAuthUrl}?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope={Uri.EscapeDataString(scopeString)}";
    }

    private async Task<string> ExchangeGitHubCodeAsync(string code, string redirectUri)
    {
        var clientId = configuration["OAuth:GitHub:ClientId"];
        var clientSecret = configuration["OAuth:GitHub:ClientSecret"];

        var tokenRequest = new { client_id = clientId, client_secret = clientSecret, code, redirect_uri = redirectUri };

        using var content = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.PostAsync(new Uri(GitHubTokenUrl), content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return tokenResponse.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Failed to get access token from GitHub");
    }

    private async Task<OAuthUserProfile> GetGitHubUserProfileAsync(string accessToken)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GameGuild");

        using var response = await httpClient.GetAsync(new Uri(GitHubUserUrl)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var user = JsonSerializer.Deserialize<GitHubUserDto>(content, _jsonOptions) ?? throw new InvalidOperationException("Failed to parse GitHub user");

        // Get email if not public
        if (string.IsNullOrEmpty(user.Email))
        {
            var primaryEmail = await GetGitHubPrimaryEmailAsync(accessToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(primaryEmail)) { user.Email = primaryEmail; }
        }

        return new OAuthUserProfile
        {
            ProviderId = user.Id.ToString(CultureInfo.InvariantCulture),
            Provider = "GitHub",
            Email = user.Email,
            EmailVerified = !string.IsNullOrEmpty(user.Email), // GitHub doesn't provide verification status
            Name = user.Name,
            Username = user.Login,
            AvatarUrl = user.AvatarUrl,
            AccessToken = accessToken
        };
    }

    private async Task<string?> GetGitHubPrimaryEmailAsync(string accessToken)
    {
        try
        {
            // Ensure auth header is set with the correct access token
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            using var emailResponse = await httpClient.GetAsync(new Uri(GitHubEmailUrl)).ConfigureAwait(false);
            emailResponse.EnsureSuccessStatusCode();

            var emailContent = await emailResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var emails = JsonSerializer.Deserialize<JsonElement[ ]>(emailContent);

            if (emails != null)
            {
                var primaryEmail = emails.Where(e => e.GetProperty("primary").GetBoolean()).Select(e => e.GetProperty("email").GetString()).FirstOrDefault();

                return primaryEmail;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch GitHub user emails");
            throw;
        }

        return null;
    }

    #endregion

    #region Google OAuth

    private string BuildGoogleAuthUrl(string clientId, string redirectUri, string state, string[ ]? scopes)
    {
        var scopeString = scopes != null && scopes.Length > 0 ? string.Join(" ", scopes) : "openid email profile";

        return $"{GoogleAuthUrl}?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope={Uri.EscapeDataString(scopeString)}" +
               $"&response_type=code" +
               $"&access_type=offline";
    }

    private async Task<string> ExchangeGoogleCodeAsync(string code, string redirectUri)
    {
        var clientId = configuration["OAuth:Google:ClientId"];
        var clientSecret = configuration["OAuth:Google:ClientSecret"];

        var tokenRequest = new Dictionary<string, string> { { "client_id", clientId! }, { "client_secret", clientSecret! }, { "code", code }, { "grant_type", "authorization_code" }, { "redirect_uri", redirectUri } };

        using var content = new FormUrlEncodedContent(tokenRequest);
        using var response = await httpClient.PostAsync(new Uri(GoogleTokenUrl), content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return tokenResponse.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Failed to get access token from Google");
    }

    private async Task<OAuthUserProfile> GetGoogleUserProfileAsync(string accessToken)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        using var response = await httpClient.GetAsync(new Uri(GoogleUserUrl)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var user = JsonSerializer.Deserialize<GoogleUserDto>(content, _jsonOptions) ?? throw new InvalidOperationException("Failed to parse Google user");

        return new OAuthUserProfile
        {
            ProviderId = user.Id,
            Provider = "Google",
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            Name = user.Name,
            FirstName = user.GivenName,
            LastName = user.FamilyName,
            AvatarUrl = user.Picture,
            Locale = null, // GoogleUserDto doesn't include locale
            AccessToken = accessToken
        };
    }

    #endregion
}
