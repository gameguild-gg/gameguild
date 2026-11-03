using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for logout command that immediately revokes tokens.
/// </summary>
public sealed class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResponse>
{
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILogger<LogoutHandler> _logger;
    private readonly IPublisher? _eventPublisher;

    public LogoutHandler(
        ITokenRevocationService tokenRevocationService,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository userSessionRepository,
        ILogger<LogoutHandler> logger,
        IPublisher? eventPublisher = null)
    {
        _tokenRevocationService = tokenRevocationService ?? throw new ArgumentNullException(nameof(tokenRevocationService));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _userSessionRepository = userSessionRepository ?? throw new ArgumentNullException(nameof(userSessionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventPublisher = eventPublisher;
    }

    public async Task<LogoutResponse> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing logout request for user: {UserId}, LogoutEverywhere: {LogoutEverywhere}",
            command.UserId, command.LogoutEverywhere);

        var sessionsInvalidated = 0;

        try
        {
            if (command.LogoutEverywhere)
            {
                // Revoke ALL user tokens immediately (for access token validation)
                await _tokenRevocationService.RevokeAllUserTokensAsync(
                    command.UserId,
                    command.Reason ?? "User initiated logout everywhere",
                    cancellationToken).ConfigureAwait(false);

                // Revoke all refresh tokens in database
                await _refreshTokenRepository.RevokeAllForUserAsync(
                    command.UserId,
                    command.IpAddress,
                    cancellationToken).ConfigureAwait(false);

                // In-process refresh-token lifecycle telemetry (issue #1199): logout-everywhere
                // revokes the account's whole refresh-token set, so the token id is the "all"
                // sentinel rather than a single row id.
                await AuthenticationEventDispatch.PublishAsync(
                    _eventPublisher,
                    new RefreshTokenRevokedEvent("all", command.UserId, command.Reason ?? "LogoutEverywhere", SystemClock.UtcNow),
                    _logger,
                    cancellationToken).ConfigureAwait(false);

                // Terminate all user sessions
                await _userSessionRepository.TerminateAllForUserAsync(
                    command.UserId, 
                    command.Reason ?? "User initiated logout everywhere",
                    cancellationToken).ConfigureAwait(false);

                // Count sessions for response
                var sessions = await _userSessionRepository.GetByUserIdAsync(command.UserId, cancellationToken).ConfigureAwait(false);
                sessionsInvalidated = sessions.Count;

                _logger.LogInformation(
                    "User logged out everywhere: UserId={UserId}, SessionsInvalidated={Count}",
                    command.UserId, sessionsInvalidated);
            }
            else
            {
                // Revoke only the current token
                if (!string.IsNullOrEmpty(command.CurrentTokenJti))
                {
                    var expiresAt = command.CurrentTokenExpiresAt ?? SystemClock.UtcNow.AddHours(1);
                    
                    await _tokenRevocationService.RevokeTokenAsync(
                        command.CurrentTokenJti,
                        expiresAt,
                        command.Reason ?? "User initiated logout",
                        cancellationToken).ConfigureAwait(false);

                    sessionsInvalidated = 1;

                    _logger.LogInformation(
                        "Single token revoked: UserId={UserId}, JTI={Jti}",
                        command.UserId, command.CurrentTokenJti);
                }
            }

            return new LogoutResponse
            {
                Success = true,
                Message = command.LogoutEverywhere 
                    ? "Successfully logged out from all devices" 
                    : "Successfully logged out",
                SessionsInvalidated = sessionsInvalidated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", command.UserId);

            return new LogoutResponse
            {
                Success = false,
                Message = "An error occurred during logout",
                SessionsInvalidated = 0
            };
        }
    }
}
