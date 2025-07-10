using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Request DTO for verifying email
/// </summary>
public class VerifyEmailRequestDto {
  [Required] public string Token { get; set; } = string.Empty;
}
