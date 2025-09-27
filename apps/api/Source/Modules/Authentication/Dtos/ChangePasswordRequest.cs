namespace GameGuild.Modules.Authentication;

/// <summary> Request DTO for changing password (authenticated user) </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
