using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for updating an existing user
/// </summary>
public class UpdateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);

        if (user == null) throw new UserNotFoundException();

        // Update user properties
        user.UpdateName(request.Name);
        if (request.PhoneNumber != null) user.UpdatePhoneNumber(request.PhoneNumber);

        // Save changes
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Return updated user DTO
        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber);
    }
}
