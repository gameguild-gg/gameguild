namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for revoke token command using CQRS pattern
/// </summary>
public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Unit> {
  private readonly IAuthService _authService;
  private readonly IMediator _mediator;
  private readonly ILogger<RevokeTokenHandler> _logger;

  public RevokeTokenHandler(IAuthService authService, IMediator mediator, ILogger<RevokeTokenHandler> logger) {
    _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<Unit> Handle(RevokeTokenCommand request, CancellationToken cancellationToken) {
    _logger.LogInformation("Processing revoke token request");

    try {
      await _authService.RevokeRefreshTokenAsync(request.RefreshToken, request.IpAddress ?? "Unknown");

      // Publish notification for audit/logging purposes
      var notification = new TokenRevokedNotification { RefreshToken = request.RefreshToken, IpAddress = request.IpAddress, RevokedAt = DateTime.UtcNow };

      await _mediator.Publish(notification, cancellationToken);

      _logger.LogInformation("Token revoked successfully");

      return Unit.Value;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to revoke token");

      throw;
    }
  }
}
