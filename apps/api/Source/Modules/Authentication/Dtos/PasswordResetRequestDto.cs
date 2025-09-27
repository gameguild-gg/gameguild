namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for password reset requests
/// </summary>
public class PasswordResetRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string? RedirectUrl { get; set; }
}
