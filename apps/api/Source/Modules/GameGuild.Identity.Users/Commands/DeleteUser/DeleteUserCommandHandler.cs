using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for deleting users (soft delete)
/// </summary>
public class DeleteUserCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) 
            ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        // Use domain method for soft delete
        user.MarkDeleted();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        // Publish domain event
        await publisher.Publish(new UserDeletedNotification(user.Id), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
