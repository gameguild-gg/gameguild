using GameGuild.CQRS;
using GameGuild.Database;


namespace GameGuild.Modules.Users;

/// <summary> Handler for getting a user by ID using Result<T> pattern </summary>
public class GetUserByIdResultHandler(ApplicationDbContext context, ILogger<GetUserByIdResultHandler> logger) : IResultQueryHandler<GetUserByIdResultQuery, User> {
  public async Task<Result<User>> Handle(GetUserByIdResultQuery request, CancellationToken cancellationToken) {
    try {
      var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

      if (user == null) {
        logger.LogDebug("User with ID {UserId} not found", request.UserId);

        return Result.NotFound<User>("Users", request.UserId);
      }

      return Result.Success(user);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error retrieving user with ID {UserId}", request.UserId);

      return Result.Failure<User>(Error.Failure("Users.RetrievalFailed", "An error occurred while retrieving the user"));
    }
  }
}
