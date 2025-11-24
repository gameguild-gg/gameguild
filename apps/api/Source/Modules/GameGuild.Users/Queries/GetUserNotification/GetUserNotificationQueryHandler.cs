using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;

namespace GameGuild.Users.Queries.GetUserNotification;

/// <summary>
/// Query handler for getting a single user notification
/// </summary>
public class GetUserNotificationQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserNotificationQuery, UserNotificationDetailDto?>
{
    public async Task<UserNotificationDetailDto?> Handle(GetUserNotificationQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        
        if (user == null)
            return null;

        // Return null since notifications are not implemented yet
        // In a real implementation, this would query a notifications repository
        return null;
    }
}
