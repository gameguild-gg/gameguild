namespace GameGuild.Modules.Authentication;

public class GoogleUserDto {
  public string Id { get; set; } = string.Empty;

  public string Email { get; set; } = string.Empty;

  public string Name { get; set; } = string.Empty;

  public string Picture { get; set; } = string.Empty;

  public bool EmailVerified { get; set; }
}
