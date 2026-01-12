using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for local sign-up command
/// </summary>
public class LocalSignUpHandler(IAuthService authService, IAuthUserRepository authUserRepository, ILogger<LocalSignUpHandler> logger) : IRequestHandler<LocalSignUpCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(LocalSignUpCommand command, CancellationToken cancellationToken)
    {
        var signUpRequest = new LocalSignUpRequest
        {
            Email = command.Email, Password = command.Password, Username = command.Username, TenantId = command.TenantId, FirstName = command.FirstName, LastName = command.LastName, PhoneNumber = command.PhoneNumber
        };

        var domainResult = await authService.LocalSignUpAsync(signUpRequest, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User successfully signed up via local authentication");

        // Map from Domain response to Application DTO
        return await domainResult.ToDto(authUserRepository, cancellationToken);
    }
}
