namespace GameGuild.Modules.Authentication;

/// <summary> Request DTO for verifying email </summary>
public class EmailVerificationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
