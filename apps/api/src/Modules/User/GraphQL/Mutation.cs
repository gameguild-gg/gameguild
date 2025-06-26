using GameGuild.Modules.User.Commands;
using GameGuild.Modules.User.Services;
using MediatR;

namespace GameGuild.Modules.User.GraphQL;

public class Mutation
{
    /// <summary>
    /// Creates a new user using CQRS pattern with MediatR.
    /// </summary>
    public async Task<Models.User> CreateUser(CreateUserInput input, [Service] IMediator mediator)
    {
        var command = new CreateUserCommand
        {
            Name = input.Name, Email = input.Email, IsActive = input.IsActive
        };

        return await mediator.Send(command);
    }

    /// <summary>
    /// Creates a new user using traditional service pattern (for comparison).
    /// </summary>
    public async Task<Models.User> CreateUserLegacy(CreateUserInput input, [Service] IUserService userService)
    {
        // Use BaseEntity constructor pattern for consistent creation
        var user = new Models.User(
            new
            {
                input.Name, input.Email, input.IsActive
            }
        );

        return await userService.CreateUserAsync(user);
    }

    /// <summary>
    /// Updates an existing user with partial data.
    /// </summary>
    public async Task<Models.User?> UpdateUser(UpdateUserInput input, [Service] IUserService userService)
    {
        Models.User? existingUser = await userService.GetUserByIdAsync(input.Id);

        if (existingUser == null)
            return null;

        // Update only provided properties
        if (!string.IsNullOrEmpty(input.Name))
            existingUser.Name = input.Name;

        if (!string.IsNullOrEmpty(input.Email))
            existingUser.Email = input.Email;

        if (input.IsActive.HasValue)
            existingUser.IsActive = input.IsActive.Value;

        return await userService.UpdateUserAsync(input.Id, existingUser);
    }

    /// <summary>
    /// Hard deletes a user by their ID (permanently removes from database).
    /// </summary>
    public async Task<bool> DeleteUser(Guid id, [Service] IUserService userService)
    {
        return await userService.DeleteUserAsync(id);
    }

    /// <summary>
    /// Soft deletes a user (marks as deleted but keeps in database).
    /// </summary>
    public async Task<bool> SoftDeleteUser(Guid id, [Service] IUserService userService)
    {
        return await userService.SoftDeleteUserAsync(id);
    }

    /// <summary>
    /// Restores a soft-deleted user.
    /// </summary>
    public async Task<bool> RestoreUser(Guid id, [Service] IUserService userService)
    {
        return await userService.RestoreUserAsync(id);
    }
}
