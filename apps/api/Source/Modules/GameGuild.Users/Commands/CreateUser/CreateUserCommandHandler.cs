using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for creating a new user
/// </summary>
public class CreateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Create new user entity
        var user = User.Create(request.Email, request.Name, request.PhoneNumber);

        // Add to repository
        await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt);
    }
}
