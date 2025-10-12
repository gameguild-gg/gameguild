using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for logging refresh token events </summary>
public class RefreshTokenEventHandler(ILogger<RefreshTokenEventHandler> logger) : INotificationHandler<RefreshTokenGeneratedEvent>,
    INotificationHandler<RefreshTokenUsedEvent>,
    INotificationHandler<RefreshTokenRevokedEvent>
{
    public async Task Handle(RefreshTokenGeneratedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh token {TokenId} generated for user {UserId}, expires at {ExpiresAt}", notification.TokenId, notification.UserId, notification.ExpiresAt);

        await Task.CompletedTask;
    }

    public async Task Handle(RefreshTokenUsedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh token {TokenId} used by user {UserId} at {UsedAt}", notification.TokenId, notification.UserId, notification.UsedAt);

        await Task.CompletedTask;
    }

    public async Task Handle(RefreshTokenRevokedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh token {TokenId} revoked for user {UserId}. Reason: {Reason} at {RevokedAt}", notification.TokenId, notification.UserId, notification.Reason, notification.RevokedAt);

        await Task.CompletedTask;
    }
}
