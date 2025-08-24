using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for restoring user
/// </summary>
public class RestoreUserHandler(
  ApplicationDbContext context,
  ILogger<RestoreUserHandler> logger,
  IMediator mediator
) : IRequestHandler<RestoreUserCommand, bool> {
  public async Task<bool> Handle(RestoreUserCommand request, CancellationToken cancellationToken) {
    var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

    if (user?.DeletedAt == null) return false;

    user.Restore();
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("User {UserId} restored", request.UserId);

    // Publish domain event
    await mediator.Publish(new UserRestoredEvent(user.Id), cancellationToken);

    return true;
  }
}
