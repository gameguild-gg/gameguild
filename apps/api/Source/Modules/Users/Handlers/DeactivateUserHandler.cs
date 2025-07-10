using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for deactivating user
/// </summary>
public class DeactivateUserHandler(ApplicationDbContext context, ILogger<DeactivateUserHandler> logger, IMediator mediator) : IRequestHandler<DeactivateUserCommand, bool> {
  public async Task<bool> Handle(DeactivateUserCommand request, CancellationToken cancellationToken) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

    if (user == null) return false;

    if (!user.IsActive) return true; // Already inactive

    user.IsActive = false;
    user.Touch();
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("User {UserId} deactivated. Reason: {Reason}", request.UserId, request.Reason ?? "Not specified");

    // Publish domain event
    await mediator.Publish(new UserDeactivatedEvent(user.Id), cancellationToken);

    return true;
  }
}
