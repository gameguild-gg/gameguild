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

            // Publish refresh token used event
            await _mediator.Publish(new RefreshTokenUsedEvent(
                response.User.Id,
                Guid.Parse(request.RefreshToken), // Note: This might need adjustment based on how RefreshToken is structured
                DateTime.UtcNow
            ), cancellationToken);

            // Publish new refresh token generated event if a new one was created
            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                await _mediator.Publish(new RefreshTokenGeneratedEvent(
                    response.User.Id,
                    Guid.NewGuid(), // This should be the actual new token ID
                    response.RefreshTokenExpiresAt ?? DateTime.UtcNow.AddDays(30),
                    DateTime.UtcNow
                ), cancellationToken);
            }

            // Optional: publish notification with extracted info
            var notification = new TokenRefreshedNotification { UserId = response.User.Id, Email = response.User.Email, TenantId = response.TenantId, RefreshedAt = DateTime.UtcNow };
            await _mediator.Publish(notification, cancellationToken);

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
