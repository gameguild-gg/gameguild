namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for forgot password requests
/// </summary>
public class ForgotPasswordRequestDto {
    public string Email { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}