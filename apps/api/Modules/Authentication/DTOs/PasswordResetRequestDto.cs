namespace GameGuild.Modules.Authentication;

/// <summary>
/// DTO for password reset requests (forgot password functionality)
/// </summary>
public class PasswordResetRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string? RedirectUrl { get; set; }
}
