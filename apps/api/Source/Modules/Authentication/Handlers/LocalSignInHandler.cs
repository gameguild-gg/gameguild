using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for local sign-in command </summary>
public class LocalSignInHandler(IAuthService authService, IMediator mediator, IHttpContextAccessor httpContextAccessor, ILogger<LocalSignInHandler> logger) : IRequestHandler<LocalSignInCommand, Result<SignInResponse>>
{
    public async Task<Result<SignInResponse>> Handle(LocalSignInCommand request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        try
        {
            var signInRequest = new LocalSignInRequest { Email = request.Email, Password = request.Password, TenantId = request.TenantId };

            var result = await authService.LocalSignInAsync(signInRequest);

            // Publish successful sign-in event
            await mediator.Publish(new UserSignedInEvent(
                result.User.Id,
                result.User.Email,
                "Local",
                ipAddress,
                userAgent,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogInformation("User {UserId} successfully signed in via local authentication", result.User.Id);

            return Result.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Publish failed authentication event
            await mediator.Publish(new AuthenticationFailedEvent(
                request.Email,
                "Invalid credentials",
                ipAddress,
                userAgent,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogWarning("Authentication failed for email {Email}: {Reason}", request.Email, ex.Message);

            return Result.Failure<SignInResponse>(Error.Problem("Authentication.InvalidCredentials", ex.Message));
        }
        catch (Exception ex)
        {
            // Publish failed authentication event
            await mediator.Publish(new AuthenticationFailedEvent(
                request.Email,
                ex.Message,
                ipAddress,
                userAgent,
                DateTime.UtcNow
            ), cancellationToken);

            logger.LogError(ex, "Sign-in failed for email {Email}", request.Email);

            return Result.Failure<SignInResponse>(Error.Failure("Authentication.SignInFailed", ex.Message));
        }
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
