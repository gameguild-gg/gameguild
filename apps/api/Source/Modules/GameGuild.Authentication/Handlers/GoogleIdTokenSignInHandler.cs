using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Mappings;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using DomainRequests = GameGuild.Authentication.Models.Requests;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace GameGuild.Authentication.Handlers;

/// <summary>
///     Handler for Google ID token sign-in command
/// </summary>
public class GoogleIdTokenSignInHandler(IAuthService authService, IAuthUserRepository authUserRepository, ILogger<GoogleIdTokenSignInHandler> logger, FluentValidation.IValidator<GoogleIdTokenSignInCommand> validator)
    : IRequestHandler<GoogleIdTokenSignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(GoogleIdTokenSignInCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new CQRS.ValidationError(e.PropertyName, e.ErrorMessage));

            throw new ValidationException(errors);
        }

        var signInRequest = new DomainRequests.GoogleIdTokenRequest { IdToken = command.IdToken, TenantId = command.TenantId };

        var domainResult = await authService.GoogleIdTokenSignInAsync(signInRequest, cancellationToken).ConfigureAwait(false);

        if (domainResult == null) { throw new InvalidOperationException("Authentication service returned null result"); }

        logger.LogInformation("Google ID token sign-in successful");

        // Map from Domain response to Application DTO
        return await domainResult.ToDto(authUserRepository, cancellationToken).ConfigureAwait(false);
    }
}
