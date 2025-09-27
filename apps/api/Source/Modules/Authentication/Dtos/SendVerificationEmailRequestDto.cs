namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for sending verification email requests
/// </summary>
public class SendVerificationEmailRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string? RedirectUrl { get; set; }
}
