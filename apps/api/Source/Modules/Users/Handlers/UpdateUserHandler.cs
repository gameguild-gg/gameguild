using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for updating user information
/// </summary>
public class UpdateUserHandler(ApplicationDbContext context, ILogger<UpdateUserHandler> logger, IMediator mediator) : IRequestHandler<UpdateUserCommand, User> {
  public async Task<User> Handle(UpdateUserCommand request, CancellationToken cancellationToken) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

    if (user == null) throw new InvalidOperationException($"User with ID {request.UserId} not found");

    // Optimistic concurrency control
    if (request.ExpectedVersion.HasValue && user.Version != request.ExpectedVersion.Value) throw new InvalidOperationException($"Concurrency conflict. Expected version {request.ExpectedVersion}, but current version is {user.Version}");

    // Check for email uniqueness if email is being updated
    if (request.Email != null && request.Email != user.Email) {
      var existingUser = await context.Users
                                      .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);

      if (existingUser != null) throw new InvalidOperationException($"Email {request.Email} is already in use");
    }

    // Track changes for notification
    var changes = new Dictionary<string, object>();

    // Update user properties
    if (request.Name != null && user.Name != request.Name) {
      changes["Name"] = new { From = user.Name, To = request.Name };
      user.Name = request.Name;

      // Regenerate username when name changes (only if Username is not explicitly provided)
      if (request.Username == null) {
        var baseUsername = request.Name.ToSlugCase();
        var existingUsernames = await context.Users
                                             .Where(u => u.Username.StartsWith(baseUsername) && u.Id != user.Id)
                                             .Select(u => u.Username)
                                             .ToListAsync(cancellationToken);

        var uniqueUsername = SlugCase.GenerateUnique(request.Name, existingUsernames, 50);
        changes["Username"] = new { From = user.Username, To = uniqueUsername };
        user.Username = uniqueUsername;
      }
    }

    // Handle explicit username updates
    if (request.Username != null && user.Username != request.Username) {
      // Check for username uniqueness
      var existingUser = await context.Users
                                      .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != request.UserId, cancellationToken);

      if (existingUser != null) {
        throw new InvalidOperationException($"Username {request.Username} is already in use");
      }

      // Validate username format (should be slug-like)
      if (!SlugCase.IsValidSlug(request.Username)) {
        throw new InvalidOperationException($"Username {request.Username} is not in a valid format. Use lowercase letters, numbers, and hyphens only.");
      }

      changes["Username"] = new { From = user.Username, To = request.Username };
      user.Username = request.Username;
    }

    if (request.Email != null && user.Email != request.Email) {
      changes["Email"] = new { From = user.Email, To = request.Email };
      user.Email = request.Email;
    }

    // Handle explicit username updates (not auto-generated)
    if (request.Username != null && user.Username != request.Username) {
      // Check for username uniqueness
      var existingUserWithUsername = await context.Users
                                                  .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != request.UserId, cancellationToken);

      if (existingUserWithUsername != null) {
        throw new InvalidOperationException($"Username {request.Username} is already in use");
      }

      changes["Username"] = new { From = user.Username, To = request.Username };
      user.Username = request.Username;
    }

    if (request.IsActive.HasValue && user.IsActive != request.IsActive.Value) {
      changes["IsActive"] = new { From = user.IsActive, To = request.IsActive.Value };
      user.IsActive = request.IsActive.Value;
    }

    // Only save if there are actual changes
    if (changes.Count == 0) return user;

    user.Touch();
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
      "User {UserId} updated successfully with {ChangeCount} changes",
      request.UserId,
      changes.Count
    );

    // Publish domain event
    await mediator.Publish(new UserUpdatedEvent(user.Id, changes), cancellationToken);

    return user;
  }
}
