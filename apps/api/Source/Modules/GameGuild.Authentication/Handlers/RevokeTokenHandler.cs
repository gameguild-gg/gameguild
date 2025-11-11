using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Commands;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Authentication.Handlers;

/// <summary>
///     Handler for revoke token command
/// </summary>
public class RevokeTokenHandler(IAuthService authService, ILogger<RevokeTokenHandler> logger) : IRequestHandler<RevokeTokenCommand, Unit>
{
    private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

    private readonly ILogger<RevokeTokenHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Unit> Handle(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing revoke token request");

        await _authService.RevokeRefreshTokenAsync(command.RefreshToken, command.IpAddress ?? "Unknown", cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Token revoked successfully");

        return Unit.Value;
    }
}
