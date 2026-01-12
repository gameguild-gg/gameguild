using System.Text.RegularExpressions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for polymorphic sign-in command supporting multiple credential types
/// </summary>
public class PolymorphicSignInHandler(
    IAuthService authService,
    IAuthUserRepository authUserRepository,
    ILogger<PolymorphicSignInHandler> logger,
    FluentValidation.IValidator<PolymorphicSignInCommand>? validator = null
) : IRequestHandler<PolymorphicSignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(PolymorphicSignInCommand command, CancellationToken cancellationToken)
    {
        // Validate command if validator is available
        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage));
                throw new ValidationException(errors);
            }
        }

        // Auto-detect credential type if not specified
        var credentialType = command.CredentialType ?? DetectCredentialType(command.Credential);

        logger.LogInformation("Processing polymorphic sign-in for credential type: {CredentialType}", credentialType);

        // For now, treat all credential types as email and use local sign-in
        // This can be extended to support phone and username in the future
        var localSignInRequest = new LocalSignInRequest
        {
            Email = command.Credential,
            Password = command.Password,
            TenantId = command.TenantId
        };

        var domainResult = await authService.LocalSignInAsync(localSignInRequest, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Polymorphic sign-in successful");

        // Map from Domain response to Application DTO
        return await domainResult.ToDto(authUserRepository, cancellationToken).ConfigureAwait(false);
    }

    private static CredentialType DetectCredentialType(string credential)
    {
        // Simple email detection
        if (Regex.IsMatch(credential, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return CredentialType.Email;
        }

        // Simple phone detection (starts with + and contains only digits)
        if (Regex.IsMatch(credential, @"^\+?\d+$"))
        {
            return CredentialType.Phone;
        }

        // Default to username
        return CredentialType.Username;
    }
}
