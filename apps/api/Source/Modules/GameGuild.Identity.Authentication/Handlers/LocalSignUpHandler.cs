using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for local sign-up command
/// </summary>
public sealed class LocalSignUpHandler(
    IAuthService authService,
    IUserRepository userRepository,
    ISender sender,
    ILogger<LocalSignUpHandler> logger,
    IGuestSessionService? guestSessionService = null,
    ITurnstileVerificationService? turnstileVerification = null) : ICommandHandler<LocalSignUpCommand, SignInResponse>
{
    private const string VerificationSentMessage = "Sign-up successful. A verification email has been sent to your email address.";
    private const string VerificationPendingMessage = "Sign-up successful. The verification email could not be queued; please request it again from your account settings.";
    private const string TurnstileFailedMessage = "Bot verification failed. Please complete the anti-bot challenge and try again.";

    public async Task<SignInResponse> Handle(LocalSignUpCommand command, CancellationToken cancellationToken)
    {
        // Issue #1150: enforce Turnstile at the API, not only in the Next.js layer —
        // direct API callers (curl/scripts/attackers) bypassed the web check entirely.
        // Enforced exactly when a Turnstile secret is configured (see the verifier:
        // Disabled/VerifyUnavailable fail open, MissingToken/VerifyFailed fail closed).
        if (turnstileVerification is not null
            && !(await turnstileVerification.VerifyAsync(command.TurnstileToken, null, cancellationToken).ConfigureAwait(false)).Success)
        {
            logger.LogWarning("Sign-up rejected: Turnstile verification failed");
            return new SignInResponse { Success = false, Message = TurnstileFailedMessage };
        }

        var signUpRequest = new LocalSignUpRequest
        {
            Email = command.Email, Password = command.Password, Username = command.Username, TenantId = command.TenantId, FirstName = command.FirstName, LastName = command.LastName, PhoneNumber = command.PhoneNumber
        };

        var domainResult = await authService.LocalSignUpAsync(signUpRequest, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User successfully signed up via local authentication");

        // Map from Domain response to Application DTO
        var response = await domainResult.ToDto(userRepository, cancellationToken).ConfigureAwait(false);

        // A failed sign-up must never trigger a verification email.
        if (!response.Success)
        {
            return response;
        }

        try
        {
            await sender.Send(new SendEmailVerificationCommand
            {
                Email = response.Email, UserId = response.UserId, UserName = command.Username
            }, cancellationToken).ConfigureAwait(false);

            response.Message = VerificationSentMessage;
            logger.LogInformation("Sign-up verification email dispatched for user {UserId}", response.UserId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The account is already committed; an unavailable notification queue must not fail the sign-up.
            // The user can request a new verification email through the resend endpoint.
            logger.LogWarning(ex, "Sign-up verification email could not be queued for user {UserId}; signup remains successful", response.UserId);
            response.Message = VerificationPendingMessage;
        }

        // Session continuity (issue #213): a guest registering from an anonymous session
        // migrates that session into the issued authenticated session.
        return guestSessionService is null
            ? response
            : await GuestSessionUpgradeIntegration.ApplyGuestUpgradeAsync(response, guestSessionService, command.GuestAccessToken, cancellationToken).ConfigureAwait(false);
    }
}
