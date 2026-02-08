using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for social sign-in command
/// </summary>
public class SocialSignInHandler(
    IAuthService authService,
    IUserRepository userRepository,
    ILogger<SocialSignInHandler> logger,
    FluentValidation.IValidator<SocialSignInCommand>? validator = null
) : IRequestHandler<SocialSignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(SocialSignInCommand command, CancellationToken cancellationToken)
    {
        // Validate command if validator is available
        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage));
                throw new RequestValidationException(errors);
            }
        }

        logger.LogInformation("Processing social sign-in for provider: {Provider}", command.Provider);

        // Route to appropriate provider method based on provider type
        var domainResult = command.Provider switch
        {
            SocialProvider.GitHub => await authService.GitHubSignInAsync(
                new OAuthSignInRequest { AccessToken = command.Token, TenantId = command.TenantId },
                cancellationToken).ConfigureAwait(false),
            SocialProvider.Google => await authService.GoogleSignInAsync(
                new OAuthSignInRequest { AccessToken = command.Token, TenantId = command.TenantId },
                cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Social provider {command.Provider} is not supported")
        };

        logger.LogInformation("Social sign-in successful for provider: {Provider}", command.Provider);

        // Map from Domain response to Application DTO
        return await domainResult.ToDto(userRepository, cancellationToken).ConfigureAwait(false);
    }
}
