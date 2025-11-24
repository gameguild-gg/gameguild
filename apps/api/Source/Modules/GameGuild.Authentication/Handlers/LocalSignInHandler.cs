using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Commands;
using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Mappings;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DomainRequests = GameGuild.Authentication.Models.Requests;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace GameGuild.Authentication.Handlers;

/// <summary>
///     Handler for local sign-in command
/// </summary>
public class LocalSignInHandler(
    IAuthService authService,
    IAuthUserRepository authUserRepository,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalSignInHandler> logger,
    FluentValidation.IValidator<LocalSignInCommand> validator
) : IRequestHandler<LocalSignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(LocalSignInCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new CQRS.ValidationError(e.PropertyName, e.ErrorMessage));

            throw new ValidationException(errors);
        }

        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = GetClientIpAddress(httpContext);

        var signInRequest = new DomainRequests.LocalSignInRequest { Email = command.Email, Password = command.Password, TenantId = command.TenantId, DeviceFingerprint = command.DeviceFingerprint };

        var domainResult = await authService.LocalSignInAsync(signInRequest, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User successfully signed in via local authentication from IP {IpAddress}", ipAddress);

        // Map from Domain response to Application DTO
        return await domainResult.ToDto(authUserRepository, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(forwarded)) { return forwarded.Split(',')[0].Trim(); }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
