using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Request DTO for changing password (authenticated user)
/// </summary>
public class ChangePasswordRequestDto {
  [Required] public string CurrentPassword { get; set; } = string.Empty;

  [Required] [MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}
