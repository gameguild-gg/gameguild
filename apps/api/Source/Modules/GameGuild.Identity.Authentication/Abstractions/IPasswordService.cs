namespace GameGuild.Identity.Authentication;

/// <summary>
/// Service interface for password management: forgot-password, reset-password, change-password, email verification
/// </summary>
public interface IPasswordService
{
    Task<EmailOperationResponse> SendEmailVerificationAsync(SendEmailVerificationRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> VerifyEmailAsync(EmailVerificationRequest verificationRequest, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ForgotPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<EmailOperationResponse> ChangePasswordAsync(ChangePasswordRequest request, Guid userId, CancellationToken cancellationToken = default);
}
