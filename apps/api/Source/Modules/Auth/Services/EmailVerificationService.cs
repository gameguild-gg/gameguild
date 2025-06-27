using System.Security.Cryptography;
using System.Text;
using GameGuild.Data;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.User.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Auth.Services {
  public interface IEmailVerificationService {
    Task<EmailOperationResponseDto> SendEmailVerificationAsync(string email);

    Task<EmailOperationResponseDto> VerifyEmailAsync(string token);

    Task<EmailOperationResponseDto> SendPasswordResetAsync(string email);

    Task<EmailOperationResponseDto> ResetPasswordAsync(string token, string newPassword);

    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);

    Task<string> GeneratePasswordResetTokenAsync(Guid userId);
  }

  public class EmailVerificationService(
    ApplicationDbContext context,
    ILogger<EmailVerificationService> logger,
    IConfiguration configuration
  )
    : IEmailVerificationService {
    private readonly IConfiguration _configuration = configuration;

    // In production, use Redis or database for token storage
    private readonly Dictionary<string, TokenInfo> _tokens = new();

    public async Task<EmailOperationResponseDto> SendEmailVerificationAsync(string email) {
      try {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null) return new EmailOperationResponseDto { Success = false, Message = "User not found" };

        var token = await GenerateEmailVerificationTokenAsync(user.Id);

        // TODO: Send actual email using an email service (SendGrid, SMTP, etc.)
        // For now, just log the token (in production, never log sensitive tokens)
        logger.LogInformation("Email verification token for {Email}: {Token}", email, token);

        return new EmailOperationResponseDto { Success = true, Message = "Verification email sent successfully" };
      }
      catch (Exception ex) {
        logger.LogError(ex, "Error sending email verification to {Email}", email);

        return new EmailOperationResponseDto { Success = false, Message = "Failed to send verification email" };
      }
    }

    public async Task<EmailOperationResponseDto> VerifyEmailAsync(string token) {
      try {
        if (!_tokens.TryGetValue(token, out var tokenInfo)) return new EmailOperationResponseDto { Success = false, Message = "Invalid or expired token" };

        if (tokenInfo.ExpiresAt < DateTime.UtcNow) {
          _tokens.Remove(token);

          return new EmailOperationResponseDto { Success = false, Message = "Token has expired" };
        }

        if (tokenInfo.Type != "email_verification") return new EmailOperationResponseDto { Success = false, Message = "Invalid token type" };

        var user = await context.Users.FindAsync(tokenInfo.UserId);

        if (user == null) return new EmailOperationResponseDto { Success = false, Message = "User not found" };

        // Mark email as verified (you might want to add an EmailVerified field to User model)
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Remove used token
        _tokens.Remove(token);

        return new EmailOperationResponseDto { Success = true, Message = "Email verified successfully" };
      }
      catch (Exception ex) {
        logger.LogError(ex, "Error verifying email with token {Token}", token);

        return new EmailOperationResponseDto { Success = false, Message = "Email verification failed" };
      }
    }

    public async Task<EmailOperationResponseDto> SendPasswordResetAsync(string email) {
      try {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
          // Don't reveal if user exists or not for security
          return new EmailOperationResponseDto { Success = true, Message = "If an account with that email exists, a password reset link has been sent" };

        var token = await GeneratePasswordResetTokenAsync(user.Id);

        // TODO: Send actual email using an email service
        // For now, just log the token (in production, never log sensitive tokens)
        logger.LogInformation("Password reset token for {Email}: {Token}", email, token);

        return new EmailOperationResponseDto { Success = true, Message = "If an account with that email exists, a password reset link has been sent" };
      }
      catch (Exception ex) {
        logger.LogError(ex, "Error sending password reset to {Email}", email);

        return new EmailOperationResponseDto { Success = false, Message = "Failed to send password reset email" };
      }
    }

    public async Task<EmailOperationResponseDto> ResetPasswordAsync(string token, string newPassword) {
      try {
        if (!_tokens.TryGetValue(token, out var tokenInfo)) return new EmailOperationResponseDto { Success = false, Message = "Invalid or expired token" };

        if (tokenInfo.ExpiresAt < DateTime.UtcNow) {
          _tokens.Remove(token);

          return new EmailOperationResponseDto { Success = false, Message = "Token has expired" };
        }

        if (tokenInfo.Type != "password_reset") return new EmailOperationResponseDto { Success = false, Message = "Invalid token type" };

        var user = await context.Users.Include(u => u.Credentials)
                                 .FirstOrDefaultAsync(u => u.Id == tokenInfo.UserId);

        if (user == null) return new EmailOperationResponseDto { Success = false, Message = "User not found" };

        // Find and update password credential
        var passwordCredential = user.Credentials?.FirstOrDefault(c => c.Type == "password");

        if (passwordCredential != null) {
          passwordCredential.Value = HashPassword(newPassword);
          passwordCredential.UpdatedAt = DateTime.UtcNow;
        }
        else {
          // Create new password credential
          passwordCredential = new Credential {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = "password",
            Value = HashPassword(newPassword),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
          };

          context.Credentials.Add(passwordCredential);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Remove used token
        _tokens.Remove(token);

        return new EmailOperationResponseDto { Success = true, Message = "Password reset successfully" };
      }
      catch (Exception ex) {
        logger.LogError(ex, "Error resetting password with token {Token}", token);

        return new EmailOperationResponseDto { Success = false, Message = "Password reset failed" };
      }
    }

    public Task<string> GenerateEmailVerificationTokenAsync(Guid userId) {
      var token = GenerateSecureToken();

      var tokenInfo = new TokenInfo {
        UserId = userId, Type = "email_verification", ExpiresAt = DateTime.UtcNow.AddHours(24), // 24-hour expiration
      };

      _tokens[token] = tokenInfo;

      // Clean up expired tokens
      CleanupExpiredTokens();

      return Task.FromResult(token);
    }

    public Task<string> GeneratePasswordResetTokenAsync(Guid userId) {
      var token = GenerateSecureToken();

      var tokenInfo = new TokenInfo {
        UserId = userId, Type = "password_reset", ExpiresAt = DateTime.UtcNow.AddHours(1), // 1-hour expiration
      };

      _tokens[token] = tokenInfo;

      // Clean up expired tokens
      CleanupExpiredTokens();

      return Task.FromResult(token);
    }

    private string GenerateSecureToken() {
      using var rng = RandomNumberGenerator.Create();
      var bytes = new byte[32];
      rng.GetBytes(bytes);

      return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string HashPassword(string password) {
      // Use the same hashing logic as in AuthService
      using var sha256 = SHA256.Create();
      var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

      return Convert.ToBase64String(hashedBytes);
    }

    private void CleanupExpiredTokens() {
      var expiredKeys = _tokens.Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();

      foreach (var key in expiredKeys) _tokens.Remove(key);
    }

    private class TokenInfo {
      private Guid _userId;

      private string _type = string.Empty;

      private DateTime _expiresAt;

      public Guid UserId {
        get => _userId;
        set => _userId = value;
      }

      public string Type {
        get => _type;
        set => _type = value;
      }

      public DateTime ExpiresAt {
        get => _expiresAt;
        set => _expiresAt = value;
      }
    }
  }
}
