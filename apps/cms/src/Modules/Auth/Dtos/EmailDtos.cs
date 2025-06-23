using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Auth.Dtos
{
    /// <summary>
    /// Request DTO for sending email verification
    /// </summary>
    public class SendEmailVerificationRequestDto
    {
        private string _email = string.Empty;

        [Required]
        [EmailAddress]
        public string Email
        {
            get => _email;
            set => _email = value;
        }
    }

    /// <summary>
    /// Request DTO for verifying email
    /// </summary>
    public class VerifyEmailRequestDto
    {
        private string _token = string.Empty;

        [Required]
        public string Token
        {
            get => _token;
            set => _token = value;
        }
    }

    /// <summary>
    /// Request DTO for sending password reset email
    /// </summary>
    public class ForgotPasswordRequestDto
    {
        private string _email = string.Empty;

        [Required]
        [EmailAddress]
        public string Email
        {
            get => _email;
            set => _email = value;
        }
    }

    /// <summary>
    /// Request DTO for resetting password
    /// </summary>
    public class ResetPasswordRequestDto
    {
        private string _token = string.Empty;

        private string _newPassword = string.Empty;

        [Required]
        public string Token
        {
            get => _token;
            set => _token = value;
        }

        [Required]
        [MinLength(8)]
        public string NewPassword
        {
            get => _newPassword;
            set => _newPassword = value;
        }
    }

    /// <summary>
    /// Request DTO for changing password (authenticated user)
    /// </summary>
    public class ChangePasswordRequestDto
    {
        private string _currentPassword = string.Empty;

        private string _newPassword = string.Empty;

        [Required]
        public string CurrentPassword
        {
            get => _currentPassword;
            set => _currentPassword = value;
        }

        [Required]
        [MinLength(8)]
        public string NewPassword
        {
            get => _newPassword;
            set => _newPassword = value;
        }
    }

    /// <summary>
    /// Generic response DTO for email operations
    /// </summary>
    public class EmailOperationResponseDto
    {
        private bool _success;

        private string _message = string.Empty;

        public bool Success
        {
            get => _success;
            set => _success = value;
        }

        public string Message
        {
            get => _message;
            set => _message = value;
        }
    }
}
