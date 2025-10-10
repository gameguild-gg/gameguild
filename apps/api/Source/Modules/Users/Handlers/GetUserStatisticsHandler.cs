using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary>
///     Handler for getting user statistics
/// </summary>
public class GetUserStatisticsHandler(IUserRepository userRepository) : IQueryHandler<GetUserStatisticsQuery, UserStatistics>
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<UserStatistics> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        // If date filtering is requested but repository doesn't support it yet, use basic method
        // This can be enhanced when repository adds date filtering support
        return await _userRepository.GetUserStatisticsAsync(cancellationToken);
    }
}
