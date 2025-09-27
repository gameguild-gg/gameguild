using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for local sign-in command </summary>
public class LocalSignInHandler(IAuthService authService) : IRequestHandler<LocalSignInCommand, Result<SignInResponse>>
{
    public async Task<Result<SignInResponse>> Handle(LocalSignInCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var signInRequest = new LocalSignInRequest { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

            var result = await authService.LocalSignInAsync(signInRequest);

            return Result.Success(result);
        }
        catch (UnauthorizedAccessException ex) { return Result.Failure<SignInResponse>(Error.Problem("Authentication.InvalidCredentials", ex.Message)); }
        catch (Exception ex) { return Result.Failure<SignInResponse>(Error.Failure("Authentication.SignInFailed", ex.Message)); }
    }
}
