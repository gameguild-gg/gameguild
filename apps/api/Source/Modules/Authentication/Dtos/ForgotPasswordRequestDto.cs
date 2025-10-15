namespace GameGuild.Modules.Authentication;

/// <summary>
/// Request DTO for sending password reset email
/// </summary>
public class ForgotPasswordRequestDto {
  [Required][EmailAddress] public string Email { get; set; } = string.Empty;
}
