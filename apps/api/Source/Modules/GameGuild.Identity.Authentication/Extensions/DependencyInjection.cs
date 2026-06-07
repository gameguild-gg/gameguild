using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Dependency injection configuration for Authentication Application layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    ///     Registers all Authentication application services including command handlers and validation
    /// </summary>
    public static IServiceCollection AddAuthenticationApplication(this IServiceCollection services)
    {
        // Register Command Handlers
        services.AddScoped<IRequestHandler<LocalSignUpCommand, SignInResponse>, LocalSignUpHandler>();
        services.AddScoped<IRequestHandler<LocalSignInCommand, SignInResponse>, LocalSignInHandler>();
        services.AddScoped<IRequestHandler<RefreshTokenCommand, SignInResponse>, RefreshTokenHandler>();
        services.AddScoped<IRequestHandler<GoogleIdTokenSignInCommand, SignInResponse>, GoogleIdTokenSignInHandler>();
        services.AddScoped<IRequestHandler<SendEmailVerificationCommand, EmailVerificationResponse>, SendEmailVerificationCommandHandler>();
        services.AddScoped<IRequestHandler<VerifyEmailCommand, EmailVerificationResult>, VerifyEmailCommandHandler>();
        services.AddScoped<IRequestHandler<RequestPasswordResetCommand, PasswordResetRequestResult>, RequestPasswordResetCommandHandler>();
        services.AddScoped<IRequestHandler<ResetPasswordCommand, PasswordResetResult>, ResetPasswordCommandHandler>();
        services.AddScoped<IRequestHandler<ChangePasswordCommand, PasswordChangeResult>, ChangePasswordCommandHandler>();

        // Register Permission Template Handlers
        services.AddScoped<IQueryHandler<GetPermissionTemplatesQuery, IEnumerable<PermissionTemplateDto>>, GetPermissionTemplatesQueryHandler>();
        services.AddScoped<ICommandHandler<ApplyPermissionTemplateCommand, ApplyPermissionTemplateResult>, ApplyPermissionTemplateCommandHandler>();

        // Register validators
        services.AddScoped<FluentValidation.IValidator<LocalSignUpCommand>, LocalSignUpCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<LocalSignInCommand>, LocalSignInCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<FluentValidation.IValidator<GoogleIdTokenSignInCommand>, GoogleIdTokenSignInCommandValidator>();

        return services;
    }
}
