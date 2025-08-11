using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for updating user balance
/// </summary>
public class UpdateUserBalanceHandler(
  ApplicationDbContext context,
  ILogger<UpdateUserBalanceHandler> logger,
  IMediator mediator
) : IRequestHandler<UpdateUserBalanceCommand, User> {
  public async Task<User> Handle(UpdateUserBalanceCommand request, CancellationToken cancellationToken) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

    if (user == null) throw new InvalidOperationException($"User with ID {request.UserId} not found");

    // Validate balance logic
    if (request.AvailableBalance > request.Balance) throw new InvalidOperationException("Available balance cannot exceed total balance");

    var oldBalance = user.Balance;
    var oldAvailableBalance = user.AvailableBalance;

    user.Balance = request.Balance;
    user.AvailableBalance = request.AvailableBalance;
    user.Touch();

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
      "User {UserId} balance updated from {OldBalance}/{OldAvailable} to {NewBalance}/{NewAvailable}. Reason: {Reason}",
      request.UserId,
      oldBalance,
      oldAvailableBalance,
      request.Balance,
      request.AvailableBalance,
      request.Reason ?? "Not specified"
    );

    // Publish domain event
    await mediator.Publish(
      new UserBalanceUpdatedEvent(
        user.Id,
        oldBalance,
        request.Balance,
        oldAvailableBalance,
        request.AvailableBalance,
        request.Reason
      ),
      cancellationToken
    );

    return user;
  }
}
