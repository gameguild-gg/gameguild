using GameGuild.Common;
using GameGuild.Common.Models;
using GameGuild.Modules.Users.Inputs;
using MediatR;


namespace GameGuild.Modules.Users;

[ExtendObjectType<Mutation>]
public class UserMutations {
  /// <summary>
  /// Creates a new user using CQRS pattern
  /// </summary>
  public async Task<User> CreateUser(CreateUserInput input, [Service] IMediator mediator) {
    var command = new CreateUserCommand { Name = input.Name, Email = input.Email, IsActive = input.IsActive, InitialBalance = input.InitialBalance };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Updates an existing user using CQRS pattern
  /// </summary>
  public async Task<User> UpdateUser(UpdateUserInput input, [Service] IMediator mediator) {
    var command = new UpdateUserCommand { UserId = input.Id, Name = input.Name, Email = input.Email, IsActive = input.IsActive };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Updates user balance using CQRS pattern
  /// </summary>
  public async Task<User> UpdateUserBalance(UpdateUserBalanceInput input, [Service] IMediator mediator) {
    var command = new UpdateUserBalanceCommand {
      UserId = input.UserId,
      Balance = input.Balance,
      AvailableBalance = input.AvailableBalance,
      Reason = input.Reason,
      ExpectedVersion = input.ExpectedVersion,
    };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Deletes a user using CQRS pattern
  /// </summary>
  public async Task<bool> DeleteUser(Guid id, [Service] IMediator mediator, bool softDelete = true, string? reason = null) {
    var command = new DeleteUserCommand { UserId = id, SoftDelete = softDelete, Reason = reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Restores a soft-deleted user using CQRS pattern
  /// </summary>
  public async Task<bool> RestoreUser(Guid id, [Service] IMediator mediator, string? reason = null) {
    var command = new RestoreUserCommand { UserId = id, Reason = reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Activates a user using CQRS pattern
  /// </summary>
  public async Task<bool> ActivateUser(Guid id, [Service] IMediator mediator, string? reason = null) {
    var command = new ActivateUserCommand { UserId = id, Reason = reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Deactivates a user using CQRS pattern
  /// </summary>
  public async Task<bool> DeactivateUser(Guid id, [Service] IMediator mediator, string? reason = null) {
    var command = new DeactivateUserCommand { UserId = id, Reason = reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Bulk delete users using CQRS pattern
  /// </summary>
  public async Task<BulkOperationResult> BulkDeleteUsers(BulkDeleteUsersInput input, [Service] IMediator mediator) {
    var command = new BulkDeleteUsersCommand { UserIds = input.UserIds, SoftDelete = input.SoftDelete, Reason = input.Reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Bulk restore users using CQRS pattern
  /// </summary>
  public async Task<BulkOperationResult> BulkRestoreUsers(BulkRestoreUsersInput input, [Service] IMediator mediator) {
    var command = new BulkRestoreUsersCommand { UserIds = input.UserIds, Reason = input.Reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Bulk create users using CQRS pattern
  /// </summary>
  public async Task<BulkOperationResult> BulkCreateUsers(BulkCreateUsersInput input, [Service] IMediator mediator) {
    var command = new BulkCreateUsersCommand { Users = input.Users.Select(u => new CreateUserDto { Name = u.Name, Email = u.Email, IsActive = u.IsActive, InitialBalance = u.InitialBalance }).ToList(), Reason = input.Reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Bulk activate users using CQRS pattern
  /// </summary>
  public async Task<BulkOperationResult> BulkActivateUsers(BulkActivateUsersInput input, [Service] IMediator mediator) {
    var command = new BulkActivateUsersCommand { UserIds = input.UserIds, Reason = input.Reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Bulk deactivate users using CQRS pattern
  /// </summary>
  public async Task<BulkOperationResult> BulkDeactivateUsers(BulkDeactivateUsersInput input, [Service] IMediator mediator) {
    var command = new BulkDeactivateUsersCommand { UserIds = input.UserIds, Reason = input.Reason };

    return await mediator.Send(command);
  }

  /// <summary>
  /// Legacy method - Creates a new user using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use CreateUser with IMediator instead")]
  public async Task<User> CreateUserLegacy(CreateUserInput input, [Service] IUserService userService) {
    var user = new User(new { input.Name, input.Email, input.IsActive });

    return await userService.CreateUserAsync(user);
  }

  /// <summary>
  /// Legacy method - Updates an existing user using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use UpdateUser with IMediator instead")]
  public async Task<User?> UpdateUserLegacy(UpdateUserInput input, [Service] IUserService userService) {
    var existingUser = await userService.GetUserByIdAsync(input.Id);

    if (existingUser == null) return null;

    if (!string.IsNullOrEmpty(input.Name)) existingUser.Name = input.Name;
    if (!string.IsNullOrEmpty(input.Email)) existingUser.Email = input.Email;
    if (input.IsActive.HasValue) existingUser.IsActive = input.IsActive.Value;

    return await userService.UpdateUserAsync(input.Id, existingUser);
  }

  /// <summary>
  /// Legacy method - Hard deletes a user using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use DeleteUser with IMediator instead")]
  public async Task<bool> DeleteUserLegacy(Guid id, [Service] IUserService userService) { return await userService.DeleteUserAsync(id); }

  /// <summary>
  /// Legacy method - Soft deletes a user using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use DeleteUser with IMediator instead")]
  public async Task<bool> SoftDeleteUserLegacy(Guid id, [Service] IUserService userService) { return await userService.SoftDeleteUserAsync(id); }

  /// <summary>
  /// Legacy method - Restores a soft-deleted user using traditional service pattern (deprecated)
  /// </summary>
  [Obsolete("Use RestoreUser with IMediator instead")]
  public async Task<bool> RestoreUserLegacy(Guid id, [Service] IUserService userService) { return await userService.RestoreUserAsync(id); }
}
