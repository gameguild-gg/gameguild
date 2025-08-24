using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for deleting user
/// </summary>
public class DeleteUserHandler(
  ApplicationDbContext context,
  ILogger<DeleteUserHandler> logger,
  IMediator mediator
) : IRequestHandler<DeleteUserCommand, bool> {
  public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken) {
    var user = await context.Users
                            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

    if (user == null) throw new InvalidOperationException($"User with ID {request.UserId} not found");

    if (request.SoftDelete) {
      if (user.DeletedAt != null) return false; // Already soft deleted

      user.SoftDelete();
      logger.LogInformation("User {UserId} soft deleted", request.UserId);
    }
    else {
      context.Users.Remove(user);
      logger.LogInformation("User {UserId} permanently deleted", request.UserId);
    }

    await context.SaveChangesAsync(cancellationToken);

    // Publish domain event
    await mediator.Publish(new UserDeletedEvent(user.Id, request.SoftDelete), cancellationToken);

    return true;
  }
}
