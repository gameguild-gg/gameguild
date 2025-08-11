using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for refresh token command using CQRS pattern
/// </summary>
public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, SignInResponseDto> {
  private readonly IAuthService _authService;
  private readonly IMediator _mediator;
  private readonly ILogger<RefreshTokenHandler> _logger;

  public RefreshTokenHandler(IAuthService authService, IMediator mediator, ILogger<RefreshTokenHandler> logger) {
    _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<SignInResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) {
      _logger.LogInformation("Processing refresh token request");
      var refreshRequest = new RefreshTokenRequestDto { RefreshToken = request.RefreshToken, TenantId = request.TenantId };
      try {
        var response = await _authService.RefreshTokenAsync(refreshRequest);

        // Optional: publish notification with extracted info
        var notification = new TokenRefreshedNotification {
          UserId = response.User.Id,
          Email = response.User.Email,
          TenantId = response.TenantId,
          RefreshedAt = DateTime.UtcNow,
        };
        await _mediator.Publish(notification, cancellationToken);

        _logger.LogInformation("Refresh token processed successfully");
        return response;
      } catch (Exception ex) {
        _logger.LogError(ex, "Failed to process refresh token request");
        throw;
      }
    }
}
