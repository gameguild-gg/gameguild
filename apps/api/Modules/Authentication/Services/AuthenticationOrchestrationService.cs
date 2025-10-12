using System.Text.RegularExpressions;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Implementation of authentication orchestration service
/// </summary>
public partial class AuthenticationOrchestrationService : IAuthenticationOrchestrationService
{
    private readonly IAuthService _authService;
    private readonly IOAuthService _oauthService;
    private readonly ILogger<AuthenticationOrchestrationService> _logger;

    public AuthenticationOrchestrationService(
        IAuthService authService,
        IOAuthService oauthService,
        ILogger<AuthenticationOrchestrationService> logger)
    {
        _authService = authService;
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<SignInResponse>> PolymorphicSignInAsync(
        PolymorphicSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Detect credential type if not explicitly provided
            var credentialType = request.ExplicitType ?? DetectCredentialType(request.Credential);

            // Validate format
            var validationResult = ValidateCredentialFormat(request.Credential, credentialType);
            if (!validationResult.IsSuccess)
                return Result<SignInResponse>.Failure(validationResult.Error);

            // Route to appropriate authentication method
            var authRequest = credentialType switch
            {
                CredentialType.Email => new LocalSignInRequest
                {
                    Email = request.Credential,
                    Password = request.Password,
                    TenantId = request.TenantId
                },
                CredentialType.Phone => new LocalSignInRequest
                {
                    Email = request.Credential, // Will be handled as phone lookup
                    Password = request.Password,
                    TenantId = request.TenantId
                },
                CredentialType.Username => new LocalSignInRequest
                {
                    Email = request.Credential, // Will be handled as username lookup
                    Password = request.Password,
                    TenantId = request.TenantId
                },
                _ => throw new ArgumentException($"Unsupported credential type: {credentialType}")
            };

            var response = await _authService.LocalSignInAsync(authRequest);
            return Result<SignInResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during polymorphic sign-in for credential: {Credential}",
                request.Credential);
            return Result<SignInResponse>.Failure(Error.Failure(
                "Authentication.PolymorphicSignIn.Failed",
                "An error occurred during sign-in"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<SignInResponse>> SocialSignInAsync(
        SocialSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Route to appropriate OAuth provider
            var oauthRequest = new OAuthSignInRequest
            {
                Provider = request.Provider.ToString().ToLowerInvariant(),
                Token = request.Token,
                TenantId = request.TenantId
            };

            var response = await _oauthService.ProcessOAuthCallbackAsync(oauthRequest);
            return Result<SignInResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during social sign-in with provider: {Provider}",
                request.Provider);
            return Result<SignInResponse>.Failure(Error.Failure(
                "Authentication.SocialSignIn.Failed",
                $"Failed to sign in with {request.Provider}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<SignInResponse>> MultiStepAuthenticationAsync(
        MultiStepAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        // Placeholder for multi-step authentication flow
        // This would coordinate MFA, device trust, and risk challenges
        await Task.CompletedTask;
        return Result<SignInResponse>.Failure(Error.Failure(
            "Authentication.MultiStep.NotImplemented",
            "Multi-step authentication not yet implemented"));
    }

    /// <inheritdoc />
    public CredentialType DetectCredentialType(string credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
            throw new ArgumentException("Credential cannot be empty", nameof(credential));

        // Check for email pattern
        if (EmailRegex().IsMatch(credential))
            return CredentialType.Email;

        // Check for phone number pattern (international format)
        if (PhoneRegex().IsMatch(credential))
            return CredentialType.Phone;

        // Default to username
        return CredentialType.Username;
    }

    /// <inheritdoc />
    public Result ValidateCredentialFormat(string credential, CredentialType type)
    {
        if (string.IsNullOrWhiteSpace(credential))
            return Result.Failure(Error.Validation(
                "Authentication.Credential.Empty",
                "Credential cannot be empty"));

        return type switch
        {
            CredentialType.Email => ValidateEmail(credential),
            CredentialType.Phone => ValidatePhone(credential),
            CredentialType.Username => ValidateUsername(credential),
            _ => Result.Failure(Error.Validation(
                "Authentication.Credential.InvalidType",
                "Invalid credential type"))
        };
    }

    /// <inheritdoc />
    public async Task<Result<SignInResponse>> LinkAccountAsync(
        string userId,
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        // Placeholder for account linking functionality
        await Task.CompletedTask;
        return Result<SignInResponse>.Failure(Error.Failure(
            "Authentication.AccountLinking.NotImplemented",
            "Account linking not yet implemented"));
    }

    private static Result ValidateEmail(string email)
    {
        if (!EmailRegex().IsMatch(email))
            return Result.Failure(Error.Validation(
                "Authentication.Email.InvalidFormat",
                "Invalid email format"));

        return Result.Success();
    }

    private static Result ValidatePhone(string phone)
    {
        if (!PhoneRegex().IsMatch(phone))
            return Result.Failure(Error.Validation(
                "Authentication.Phone.InvalidFormat",
                "Invalid phone number format. Use international format (e.g., +1234567890)"));

        return Result.Success();
    }

    private static Result ValidateUsername(string username)
    {
        if (username.Length < 3 || username.Length > 50)
            return Result.Failure(Error.Validation(
                "Authentication.Username.InvalidLength",
                "Username must be between 3 and 50 characters"));

        if (!UsernameRegex().IsMatch(username))
            return Result.Failure(Error.Validation(
                "Authentication.Username.InvalidFormat",
                "Username can only contain letters, numbers, hyphens, and underscores"));

        return Result.Success();
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[1-9]\d{1,14}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9_-]{3,50}$")]
    private static partial Regex UsernameRegex();
}
