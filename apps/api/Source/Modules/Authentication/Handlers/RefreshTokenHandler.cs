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
      var refreshResponse = await _authService.RefreshTokenAsync(refreshRequest);

      // Note: RefreshTokenResponseDto may not include full user data, 
      // so we'll need to get it from the JWT token or return a simple success response
      var signInResponse = new SignInResponseDto {
        AccessToken = refreshResponse.AccessToken,
        RefreshToken = refreshResponse.RefreshToken,
        Expires = refreshResponse.ExpiresAt,
        TenantId = refreshResponse.TenantId,
        User = new UserDto(), // This would need to be populated from JWT claims or service
        AvailableTenants = new List<TenantInfoDto>()
      };

      // For now, we'll create a minimal notification without full user data
      var notification = new TokenRefreshedNotification {
        UserId = Guid.Empty, // Would need to extract from JWT or get from service
        Email = "",
        TenantId = signInResponse.TenantId,
        RefreshedAt = DateTime.UtcNow
      };

      await _mediator.Publish(notification, cancellationToken);

      _logger.LogInformation("Refresh token processed successfully");

      return signInResponse;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to process refresh token request");

      throw;
    }
  }
}
