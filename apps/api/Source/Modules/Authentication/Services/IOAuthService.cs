namespace GameGuild.Modules.Auth;

public interface IOAuthService {
  Task<GitHubUserDto> GetGitHubUserAsync(string accessToken);

  Task<GoogleUserDto> GetGoogleUserAsync(string accessToken);

  Task<GoogleUserDto> ValidateGoogleIdTokenAsync(string idToken);

  Task<string> ExchangeGitHubCodeAsync(string code, string redirectUri);

  Task<string> ExchangeGoogleCodeAsync(string code, string redirectUri);
}
