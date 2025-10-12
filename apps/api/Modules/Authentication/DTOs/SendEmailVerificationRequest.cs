namespace GameGuild.Modules.Authentication;

/// <summary> Request DTO for sending email verification </summary>
public class SendEmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
