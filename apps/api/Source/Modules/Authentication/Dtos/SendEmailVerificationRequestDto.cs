namespace GameGuild.Modules.Authentication {
  /// <summary>
  /// Request DTO for sending email verification
  /// </summary>
  public class SendEmailVerificationRequestDto {
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
  }
}
