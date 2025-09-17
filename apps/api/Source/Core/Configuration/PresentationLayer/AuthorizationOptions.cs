namespace GameGuild;

public class AuthorizationOptions {
  public string DefaultPolicy { get; set; } = "Default";

  public bool RequireAuthenticatedUser { get; set; } = true;

  public void Validate() {
    if (string.IsNullOrWhiteSpace(DefaultPolicy)) throw new InvalidOperationException("Default policy cannot be null or empty.");
  }
}
