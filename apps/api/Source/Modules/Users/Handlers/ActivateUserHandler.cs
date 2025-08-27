using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for activating user
/// </summary>
public class ActivateUserHandler(
  ApplicationDbContext context,
  ILogger<ActivateUserHandler> logger,
  IMediator mediator
) : IRequestHandler<ActivateUserCommand, bool> {
  public async Task<bool> Handle(ActivateUserCommand request, CancellationToken cancellationToken) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

    if (user == null) return false;

    if (user.IsActive) return true; // Already active

    user.IsActive = true;
    user.Touch();
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("User {UserId} activated. Reason: {Reason}", request.UserId, request.Reason ?? "Not specified");

    // Publish domain event
    await mediator.Publish(new UserActivatedEvent(user.Id), cancellationToken);

    return true;
  }
}
