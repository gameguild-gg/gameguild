namespace GameGuild.Modules.Authentication;

/// <summary> Request DTO for resetting password </summary>
public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
