using System.Security.Cryptography;
using System.Text;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.Core.Filters;

/// <summary>
///     Applies the configured account lockout policy before local password sign-in.
/// </summary>
public sealed class AuthenticationLockoutActionFilter : IAsyncActionFilter
{
    private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromHours(1);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryGetLocalSignInEmail(context, out var email))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var options = services.GetRequiredService<AuthenticationSecurityOptions>();
        var database = services.GetRequiredService<IApplicationDbContext>();
        var normalizedEmail = email.ToLowerInvariant();
        var now = DateTime.UtcNow;
        context.HttpContext.Response.Headers.CacheControl = "no-store";

        // Bound the rows read to the threshold. The stored attempt history is shared across API
        // instances, so failures are not reset by load balancing or process restart.
        var recentFailures = await database.Set<AuthenticationAttempt>()
            .Where(attempt => attempt.Email == normalizedEmail &&
                              !attempt.IsSuccessful &&
                              attempt.AttemptedAt >= now.Subtract(FailedAttemptWindow))
            .OrderByDescending(attempt => attempt.AttemptedAt)
            .Select(attempt => attempt.AttemptedAt)
            .Take(options.MaxFailedAttemptsPerHour)
            .ToListAsync(context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var lockoutEndsAt = recentFailures.Count >= options.MaxFailedAttemptsPerHour
            ? recentFailures[0].AddMinutes(options.AccountLockoutDurationMinutes)
            : DateTime.MinValue;

        if (lockoutEndsAt > now)
        {
            // Keep the response indistinguishable from invalid credentials so this check does
            // not expose whether an account exists or is currently locked.
            var identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
            services.GetService<ILogger<AuthenticationLockoutActionFilter>>()?.LogWarning(
                "Local sign-in lockout enforced for identifier hash {IdentifierHash} until {LockoutEndsAtUtc}",
                identifierHash,
                lockoutEndsAt);

            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "The email or password is incorrect."
            });
            return;
        }

        await next().ConfigureAwait(false);
    }

    private static bool TryGetLocalSignInEmail(ActionExecutingContext context, out string email)
    {
        email = string.Empty;

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
        return true;
    }
}
