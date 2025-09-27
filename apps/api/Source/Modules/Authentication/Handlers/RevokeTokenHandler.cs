using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for revoke token command using CQRS pattern </summary>
public class RevokeTokenHandler(IAuthService authService, IMediator mediator, ILogger<RevokeTokenHandler> logger) : IRequestHandler<RevokeTokenCommand, Unit>
{
    private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

    private readonly ILogger<RevokeTokenHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async Task<Unit> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing revoke token request");

        try
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, request.IpAddress ?? "Unknown");

            // Publish notification for audit/logging purposes
            var notification = new TokenRevokedNotification { RefreshToken = request.RefreshToken, IpAddress = request.IpAddress, RevokedAt = DateTime.UtcNow };

            await _mediator.Publish(notification, cancellationToken);

            _logger.LogInformation("Token revoked successfully");

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke token");

            throw;
        }
    }
}
