using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for deactivating users
/// </summary>
public class DeactivateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<DeactivateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken) ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        user.Deactivate();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt);
    }
}
