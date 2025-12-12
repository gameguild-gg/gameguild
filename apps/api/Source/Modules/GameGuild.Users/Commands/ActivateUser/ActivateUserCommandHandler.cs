using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for activating users
/// </summary>
public class ActivateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<ActivateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        user.Activate();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt);
    }
}
