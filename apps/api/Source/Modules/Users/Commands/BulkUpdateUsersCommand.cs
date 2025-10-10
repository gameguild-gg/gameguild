using GameGuild.CQRS;
using GameGuild.CQRS;

namespace GameGuild.Modules.Users.Commands;

/// <summary>
///     Represents a user update with their ID
/// </summary>
public sealed class UserUpdateData
{
    public required Guid UserId { get; init; }
    public required UpdateUserRequest UpdateData { get; init; }
}

/// <summary>
///     Command to update multiple users in bulk
/// </summary>
public sealed class BulkUpdateUsersCommand : ICommand
{
    /// <summary>
    ///     Collection of user update data
    /// </summary>
    public required IEnumerable<UserUpdateData> Updates { get; init; }
}

/// <summary>
///     Handler for BulkUpdateUsersCommand
/// </summary>
public sealed class BulkUpdateUsersCommandHandler : ICommandHandler<BulkUpdateUsersCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BulkUpdateUsersCommandHandler> _logger;

    public BulkUpdateUsersCommandHandler(
        IUserRepository userRepository,
        ILogger<BulkUpdateUsersCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(BulkUpdateUsersCommand request, CancellationToken cancellationToken)
    {
        var userIds = request.Updates.Select(u => u.UserId).ToList();
        var users = (await _userRepository.GetByIdsAsync(userIds, cancellationToken)).ToList();

        foreach (var update in request.Updates)
        {
            var user = users.FirstOrDefault(u => u.Id == update.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found during bulk update", update.UserId);
                continue;
            }

            // Apply updates
            if (update.UpdateData.GivenName != null)
                user.UpdateGivenName(update.UpdateData.GivenName);

            if (update.UpdateData.FamilyName != null)
                user.UpdateFamilyName(update.UpdateData.FamilyName);

            if (update.UpdateData.Username != null)
                user.UpdateUsername(update.UpdateData.Username);

            if (update.UpdateData.Email != null)
                user.UpdateEmail(update.UpdateData.Email);

            if (update.UpdateData.IsActive.HasValue)
            {
                if (update.UpdateData.IsActive.Value)
                    user.Activate();
                else
                    user.Deactivate();
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return Unit.Value;
    }
}
