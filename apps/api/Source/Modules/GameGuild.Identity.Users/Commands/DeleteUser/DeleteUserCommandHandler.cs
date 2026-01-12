using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for deleting users
/// </summary>
public class DeleteUserCommandHandler(IUserRepository userRepository) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
