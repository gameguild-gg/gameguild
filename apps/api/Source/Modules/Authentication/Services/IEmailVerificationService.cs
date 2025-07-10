namespace GameGuild.Modules.Auth;

public interface IEmailVerificationService {
  Task<EmailOperationResponseDto> SendEmailVerificationAsync(string email);

  Task<EmailOperationResponseDto> VerifyEmailAsync(string token);

  Task<EmailOperationResponseDto> SendPasswordResetAsync(string email);

  Task<EmailOperationResponseDto> ResetPasswordAsync(string token, string newPassword);

  Task<string> GenerateEmailVerificationTokenAsync(Guid userId);

  Task<string> GeneratePasswordResetTokenAsync(Guid userId);
}
