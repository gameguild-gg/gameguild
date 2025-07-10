using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for deleting user
/// </summary>
public class DeleteUserHandler(
  ApplicationDbContext context,
  ILogger<DeleteUserHandler> logger,
  IMediator mediator) : IRequestHandler<DeleteUserCommand, bool>
{
  public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
  {
    var user = await context.Users
                            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

    if (user == null) return false;

    if (request.SoftDelete)
    {
      if (user.DeletedAt != null) return false; // Already soft deleted

      user.SoftDelete();
      logger.LogInformation("User {UserId} soft deleted", request.UserId);
    }
    else
    {
      context.Users.Remove(user);
      logger.LogInformation("User {UserId} permanently deleted", request.UserId);
    }

    await context.SaveChangesAsync(cancellationToken);

    // Publish notification
    await mediator.Publish(new UserDeletedNotification
    {
      UserId = user.Id,
      DeletedAt = DateTime.UtcNow,
      SoftDelete = request.SoftDelete
    }, cancellationToken);

    return true;
  }
}
