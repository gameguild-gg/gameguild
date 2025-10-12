using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for refresh token command using CQRS pattern </summary>
public class RefreshTokenHandler(IAuthService authService, IMediator mediator, ILogger<RefreshTokenHandler> logger) : IRequestHandler<RefreshTokenCommand, SignInResponse>
{
    private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

    private readonly ILogger<RefreshTokenHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async Task<SignInResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing refresh token request");
        var refreshRequest = new RefreshTokenRequest { RefreshToken = request.RefreshToken, TenantId = request.TenantId };

        try
        {
            var response = await _authService.RefreshTokenAsync(refreshRequest);

            // Note: We don't publish RefreshTokenUsedEvent and RefreshTokenGeneratedEvent here
            // because these should be published from within the AuthService.RefreshTokenAsync method
            // where the actual token operations happen and we have access to the token entities

            // Publish event for successful token refresh (for analytics and logging)
            var tokenRefreshedEvent = new TokenRefreshedEvent(response.User.Id, response.User.Email, response.TenantId, DateTime.UtcNow);
            await _mediator.Publish(tokenRefreshedEvent, cancellationToken);

            _logger.LogInformation("Refresh token processed successfully for user {UserId}", response.User.Id);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process refresh token request");

            throw;
        }
    }
}
