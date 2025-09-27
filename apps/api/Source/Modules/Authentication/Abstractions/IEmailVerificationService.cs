namespace GameGuild.Modules.Authentication;

public interface IEmailVerificationService
{
    Task<EmailOperationResponse> SendEmailVerificationAsync(string email);

    Task<EmailOperationResponse> VerifyEmailAsync(string token);

    Task<EmailOperationResponse> SendPasswordResetAsync(string email);

    Task<EmailOperationResponse> ResetPasswordAsync(string token, string newPassword);

    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);

    Task<string> GeneratePasswordResetTokenAsync(Guid userId);
}
