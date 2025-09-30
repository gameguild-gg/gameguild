using GameGuild.CQRS;

namespace GameGuild.Modules.Authentication;

/// <summary> Handler for logging user sign-in events </summary>
public class UserSignedInEventHandler(ILogger<UserSignedInEventHandler> logger) : INotificationHandler<UserSignedInEvent>
{
    public async Task Handle(UserSignedInEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "User {UserId} ({Email}) signed in via {SignInMethod} from {IpAddress} at {SignedInAt}",
            notification.UserId,
            notification.Email,
            notification.SignInMethod,
            notification.IpAddress ?? "Unknown",
            notification.SignedInAt
        );

        // Here you could also:
        // - Update user last login timestamp
        // - Track authentication metrics
        // - Send notifications for unusual login patterns
        // - Update session tracking

        await Task.CompletedTask;
    }
}
