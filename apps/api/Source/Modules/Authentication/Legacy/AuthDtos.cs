namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for Google ID token requests
/// </summary>
public class GoogleIdTokenRequestDto {
    public string IdToken { get; set; } = string.Empty;
}