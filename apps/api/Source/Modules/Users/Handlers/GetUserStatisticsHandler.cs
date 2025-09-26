using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for getting user statistics
/// </summary>
public class GetUserStatisticsHandler(IUserRepository userRepository) : IQueryHandler<GetUserStatisticsQuery, UserStatistics>
{
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<UserStatistics> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        // For now, use the basic statistics method - we can enhance this later for date filtering
        return await _userRepository.GetUserStatisticsAsync(cancellationToken);
    }
}
