using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk deleting users
/// </summary>
public class BulkDeleteUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkDeleteUsersCommand>
{
    public async Task<Unit> Handle(BulkDeleteUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);

        foreach (var user in users) { await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false); }

        return Unit.Value;
    }
}
