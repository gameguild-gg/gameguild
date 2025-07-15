using GameGuild.Modules.Authentication;


namespace GameGuild.Tests.Infrastructure.Integration;

/// <summary>
/// Mock implementation of IAuthService for integration testing
/// Provides stub implementations that return default/test values
/// </summary>
public class MockAuthService : IAuthService
{
    public Task<SignInResponseDto> LocalSignInAsync(LocalSignInRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-access-token",
            RefreshToken = "mock-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = request.Email ?? "test@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<SignInResponseDto> LocalSignUpAsync(LocalSignUpRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-access-token",
            RefreshToken = "mock-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = request.Email ?? "test@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        return Task.FromResult(new RefreshTokenResponseDto
        {
            AccessToken = "mock-new-access-token",
            RefreshToken = "mock-new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        });
    }

    public Task RevokeRefreshTokenAsync(string token, string ipAddress)
    {
        return Task.CompletedTask;
    }

    public Task<SignInResponseDto> GitHubSignInAsync(OAuthSignInRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-github-token",
            RefreshToken = "mock-github-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = "github@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<SignInResponseDto> GoogleSignInAsync(OAuthSignInRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-google-token",
            RefreshToken = "mock-google-refresh-token", 
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = "google@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<SignInResponseDto> GoogleIdTokenSignInAsync(GoogleIdTokenRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-google-id-token",
            RefreshToken = "mock-google-id-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = "google-id@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<string> GetGitHubAuthUrlAsync(string redirectUri)
    {
        return Task.FromResult($"https://github.com/login/oauth/authorize?redirect_uri={redirectUri}&mock=true");
    }

    public Task<string> GetGoogleAuthUrlAsync(string redirectUri)
    {
        return Task.FromResult($"https://accounts.google.com/oauth/authorize?redirect_uri={redirectUri}&mock=true");
    }

    public Task<Web3ChallengeResponseDto> GenerateWeb3ChallengeAsync(Web3ChallengeRequestDto request)
    {
        return Task.FromResult(new Web3ChallengeResponseDto
        {
            Challenge = "mock-web3-challenge-string",
            Nonce = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
    }

    public Task<SignInResponseDto> VerifyWeb3SignatureAsync(Web3VerifyRequestDto request)
    {
        return Task.FromResult(new SignInResponseDto
        {
            AccessToken = "mock-web3-token",
            RefreshToken = "mock-web3-refresh-token",
            Expires = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto { Id = Guid.NewGuid(), Email = "web3@example.com" },
            TenantId = Guid.NewGuid()
        });
    }

    public Task<EmailOperationResponseDto> SendEmailVerificationAsync(SendEmailVerificationRequestDto request)
    {
        return Task.FromResult(new EmailOperationResponseDto
        {
            Success = true,
            Message = "Mock email verification sent"
        });
    }

    public Task<EmailOperationResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        return Task.FromResult(new EmailOperationResponseDto
        {
            Success = true,
            Message = "Mock email verification successful"
        });
    }

    public Task<EmailOperationResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        return Task.FromResult(new EmailOperationResponseDto
        {
            Success = true,
            Message = "Mock password reset email sent"
        });
    }

    public Task<EmailOperationResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return Task.FromResult(new EmailOperationResponseDto
        {
            Success = true,
            Message = "Mock password reset successful"
        });
    }

    public Task<EmailOperationResponseDto> ChangePasswordAsync(ChangePasswordRequestDto request, Guid userId)
    {
        return Task.FromResult(new EmailOperationResponseDto
        {
            Success = true,
            Message = "Mock password change successful"
        });
    }
}
