using GameGuild.CQRS;
using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary> Enhanced handler for creating a new user using Result<T> pattern for better error handling </summary>
public class CreateUserResultHandler(ApplicationDbContext context, ILogger<CreateUserResultHandler> logger, IMediator mediator) : IResultCommandHandler<CreateUserResultCommand, User> {
  public async Task<Result<User>> Handle(CreateUserResultCommand request, CancellationToken cancellationToken) {
    try {
      // Check if email already exists
      var existingUser = await context.Users.FirstOrDefaultAsync(user => user.Email == request.Email, cancellationToken);

      if (existingUser != null) { return Result.Failure<User>(Error.Conflict("Users.EmailExists", $"User with email {request.Email} already exists")); }

      // Generate unique username from name using slugify
      var baseUsername = request.Name.ToSlugCase();
      var existingUsernames = await context.Users.Where(u => u.Username.StartsWith(baseUsername)).Select(u => u.Username).ToListAsync(cancellationToken);

      var uniqueUsername = SlugCase.GenerateUnique(request.Name, existingUsernames, 50);

      // Normalize negative balance to zero - business rule
      var normalizedBalance = Math.Max(0, request.InitialBalance);

      var user = new User { Name = request.Name, Username = uniqueUsername, Email = request.Email, IsActive = request.IsActive, Balance = Money.FromDecimal(normalizedBalance), AvailableBalance = Money.FromDecimal(normalizedBalance) };

      context.Users.Add(user);
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("User {UserId} created with email {Email}", user.Id, user.Email);

      // Publish domain event
      await mediator.Publish(new UserCreatedEvent(user.Id, user.Email, user.Name, user.CreatedAt), cancellationToken);

      return Result.Success(user);
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true) {
      logger.LogWarning(ex, "Attempted to create user with duplicate data");

      return Result.Failure<User>(Error.Conflict("Users.DuplicateData", "A user with this information already exists"));
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error creating user with email {Email}", request.Email);

      return Result.Failure<User>(Error.Failure("Users.CreateFailed", "An error occurred while creating the user"));
    }
  }
}
