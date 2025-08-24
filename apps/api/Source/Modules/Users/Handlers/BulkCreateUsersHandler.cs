using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for bulk creating users
/// </summary>
public class BulkCreateUsersHandler(
  ApplicationDbContext context,
  ILogger<BulkCreateUsersHandler> logger,
  IMediator mediator
) : ICommandHandler<BulkCreateUsersCommand, BulkOperationResult> {
  public async Task<BulkOperationResult> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken) {
    var createdUsers = new List<User>();
    var errors = new List<string>();
    var successfulCount = 0;

    foreach (var userDto in request.Users) {
      try {
        // Check if user with email already exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email, cancellationToken);

        if (existingUser != null) {
          errors.Add($"User with email {userDto.Email} already exists");

          continue;
        }

        // Generate unique username from name using slugify
        var baseUsername = userDto.Name.ToSlugCase();
        var existingUsernames = await context.Users
                                             .Where(u => u.Username.StartsWith(baseUsername))
                                             .Select(u => u.Username)
                                             .ToListAsync(cancellationToken);

        var uniqueUsername = SlugCase.GenerateUnique(userDto.Name, existingUsernames, 50);

        var user = new User {
          Name = userDto.Name,
          Username = uniqueUsername,
          Email = userDto.Email,
          IsActive = userDto.IsActive,
          Balance = userDto.InitialBalance,
          AvailableBalance = userDto.InitialBalance,
        };

        context.Users.Add(user);
        createdUsers.Add(user);
        successfulCount++;
      }
      catch (Exception ex) {
        errors.Add($"Failed to create user with email {userDto.Email}: {ex.Message}");
        logger.LogError(ex, "Failed to create user with email {Email}", userDto.Email);
      }
    }

    if (createdUsers.Count != 0) {
      await context.SaveChangesAsync(cancellationToken);

      // Publish domain events for created users
      foreach (var user in createdUsers) await mediator.Publish(new UserCreatedEvent(user.Id, user.Email, user.Name, user.CreatedAt), cancellationToken);
    }

    var result = new BulkOperationResult(request.Users.Count, successfulCount, errors.Count);

    foreach (var error in errors) result.AddError(error);

    logger.LogInformation(
      "Bulk create completed: {Successful}/{Total} users created. Reason: {Reason}",
      successfulCount,
      request.Users.Count,
      request.Reason ?? "Not specified"
    );

    return result;
  }
}
