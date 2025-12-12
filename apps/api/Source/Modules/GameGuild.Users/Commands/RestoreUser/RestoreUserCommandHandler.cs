using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Events;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for restoring soft-deleted users
/// </summary>
public class RestoreUserCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<RestoreUserCommand, UserDto>
{
    public async Task<UserDto> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        // Restore the user (undelete)
        user.Restore();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        // Publish domain event
        await publisher.Publish(new UserRestoredNotification(user.Id, user.Email, user.Name), cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt);
    }
}
