namespace GameGuild.Modules.Authentication;

/// <summary>
/// DTO for email verification requests
/// </summary>
public class SendEmailVerificationRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string? RedirectUrl { get; set; }
}
