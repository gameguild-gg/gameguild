using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild.API.Core.Filters;

/// <summary>Applies shared password admission before the email sign-in action.</summary>
public sealed class AuthenticationLockoutActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryGetLocalSignIn(context, out var email, out var turnstileToken))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        cancellationToken.ThrowIfCancellationRequested();
        var origin = AuthenticationTimingOrigin.GetOrStartForRequest(context.HttpContext, services.GetService<TimeProvider>());
        IAsyncDisposable lease;
        try
        {
            lease = await services.GetRequiredService<IPasswordSignInAdmissionService>()
                .AdmitAsync(email, context.HttpContext, origin, cancellationToken, turnstileToken).ConfigureAwait(false);
        }
        catch (PasswordSignInCaptchaRequiredException exception)
        {
            // Issue #1082: the stable marker in the detail lets the client reveal the
            // CAPTCHA challenge; the status stays the generic 401 used for denied sign-ins.
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = exception.Message
            });
            return;
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = exception.Message
            });
            return;
        }

        await using var heldLock = lease;
        await next().ConfigureAwait(false);
    }

    private static bool TryGetLocalSignIn(ActionExecutingContext context, out string email, out string? turnstileToken)
    {
        email = string.Empty;
        turnstileToken = null;

        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor ||
            descriptor.ControllerTypeInfo.AsType() != typeof(AuthController) ||
            !string.Equals(descriptor.ActionName, nameof(AuthController.LocalSignIn), StringComparison.Ordinal))
        {
            return false;
        }

        var request = context.ActionArguments.Values.OfType<LocalSignInRequest>().FirstOrDefault();
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            return false;
        }

        email = request.Email;
        turnstileToken = request.TurnstileToken;
        return true;
    }
}
