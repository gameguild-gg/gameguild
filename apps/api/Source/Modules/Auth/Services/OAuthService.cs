using System.Text.Json;
using GameGuild.Modules.Auth.Dtos;


namespace GameGuild.Modules.Auth.Services {
  public interface IOAuthService {
    Task<GitHubUserDto> GetGitHubUserAsync(string accessToken);

    Task<GoogleUserDto> GetGoogleUserAsync(string accessToken);

    Task<GoogleUserDto> ValidateGoogleIdTokenAsync(string idToken);

    Task<string> ExchangeGitHubCodeAsync(string code, string redirectUri);

    Task<string> ExchangeGoogleCodeAsync(string code, string redirectUri);
  }

  public class OAuthService : IOAuthService {
    private readonly HttpClient _httpClient;

    private readonly IConfiguration _configuration;

    public OAuthService(HttpClient httpClient, IConfiguration configuration) {
      _httpClient = httpClient;
      _configuration = configuration;
    }

    public async Task<string> ExchangeGitHubCodeAsync(string code, string redirectUri) {
      string? clientId = _configuration["OAuth:GitHub:ClientId"];
      string? clientSecret = _configuration["OAuth:GitHub:ClientSecret"];

      var tokenRequest = new { client_id = clientId, client_secret = clientSecret, code, redirect_uri = redirectUri };

      var content = new StringContent(
        JsonSerializer.Serialize(tokenRequest),
        System.Text.Encoding.UTF8,
        "application/json"
      );

      _httpClient.DefaultRequestHeaders.Accept.Clear();
      _httpClient.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
      );

      HttpResponseMessage response =
        await _httpClient.PostAsync("https://github.com/login/oauth/access_token", content);
      string responseContent = await response.Content.ReadAsStringAsync();

      var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

      return tokenResponse.GetProperty("access_token").GetString() ??
             throw new InvalidOperationException("Failed to get access token");
    }

    public async Task<GitHubUserDto> GetGitHubUserAsync(string accessToken) {
      _httpClient.DefaultRequestHeaders.Clear();
      _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
      _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameGuild-CMS");

      HttpResponseMessage response = await _httpClient.GetAsync("https://api.github.com/user");
      string content = await response.Content.ReadAsStringAsync();

      GitHubUserDto user =
        JsonSerializer.Deserialize<GitHubUserDto>(
          content,
          new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
        ) ??
        throw new InvalidOperationException("Failed to parse GitHub user");

      // Get email if not public
      if (string.IsNullOrEmpty(user.Email)) {
        HttpResponseMessage emailResponse = await _httpClient.GetAsync("https://api.github.com/user/emails");
        string emailContent = await emailResponse.Content.ReadAsStringAsync();
        var emails = JsonSerializer.Deserialize<JsonElement[]>(emailContent);

        if (emails != null) {
          foreach (JsonElement email in emails) {
            if (email.GetProperty("primary").GetBoolean()) {
              user.Email = email.GetProperty("email").GetString() ?? "";

              break;
            }
          }
        }
      }

      return user;
    }

    public async Task<string> ExchangeGoogleCodeAsync(string code, string redirectUri) {
      string? clientId = _configuration["OAuth:Google:ClientId"];
      string? clientSecret = _configuration["OAuth:Google:ClientSecret"];

      var tokenRequest = new Dictionary<string, string> {
        { "client_id", clientId! },
        { "client_secret", clientSecret! },
        { "code", code },
        { "grant_type", "authorization_code" },
        { "redirect_uri", redirectUri }
      };

      var content = new FormUrlEncodedContent(tokenRequest);
      HttpResponseMessage response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
      string responseContent = await response.Content.ReadAsStringAsync();

      var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

      return tokenResponse.GetProperty("access_token").GetString() ??
             throw new InvalidOperationException("Failed to get access token");
    }

    public async Task<GoogleUserDto> GetGoogleUserAsync(string accessToken) {
      _httpClient.DefaultRequestHeaders.Clear();
      _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      HttpResponseMessage response = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
      string content = await response.Content.ReadAsStringAsync();

      return JsonSerializer.Deserialize<GoogleUserDto>(
               content,
               new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
             ) ??
             throw new InvalidOperationException("Failed to parse Google user");
    }

    public async Task<GoogleUserDto> ValidateGoogleIdTokenAsync(string idToken) {
      try {
        // Call Google's tokeninfo API to validate the ID token
        HttpResponseMessage response =
          await _httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");

        if (!response.IsSuccessStatusCode) { throw new UnauthorizedAccessException($"Google tokeninfo API returned {response.StatusCode}"); }

        string content = await response.Content.ReadAsStringAsync();

        // Parse the JSON response manually since Google's tokeninfo API uses different field names
        var tokenInfo = JsonSerializer.Deserialize<JsonElement>(content);

        // Validate that the token is valid
        if (!tokenInfo.TryGetProperty("aud", out _)) { throw new UnauthorizedAccessException("Invalid Google ID token: missing audience"); }

        // Extract user information and map to GoogleUserDto
        var googleUser = new GoogleUserDto {
          Id = tokenInfo.GetProperty("sub").GetString() ?? throw new InvalidOperationException("Missing sub claim"),
          Email = tokenInfo.GetProperty("email").GetString() ??
                  throw new InvalidOperationException("Missing email claim"),
          Name = tokenInfo.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? "" : "",
          Picture = tokenInfo.TryGetProperty("picture", out JsonElement pictureElement)
                      ? pictureElement.GetString() ?? ""
                      : "",
          EmailVerified = tokenInfo.TryGetProperty("email_verified", out JsonElement verifiedElement) &&
                          (verifiedElement.GetString() == "true" || verifiedElement.ValueKind == JsonValueKind.True)
        };

        return googleUser;
      }
      catch (Exception ex) when (!(ex is UnauthorizedAccessException)) { throw new UnauthorizedAccessException($"Failed to validate Google ID token: {ex.Message}", ex); }
    }
  }
}
