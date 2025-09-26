using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Handler for creating a new user with validation and business logic </summary>
public class CreateUserHandler(IUserService userService, ILogger<CreateUserHandler> logger, IMediator mediator) : IRequestHandler<CreateUserCommand, User>
{
  public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
  {
    logger.LogDebug("Creating user with email {Email}", request.Email);

    User user = await userService.CreateUserAsync(request.GivenName, request.FamilyName, request.Email, request.IsActive, cancellationToken);

    logger.LogInformation("User {UserId} created with email {Email}", user.Id, user.Email);

    // Publish domain event
    await mediator.Publish(new UserCreatedEvent(user.Id, user.Email, user.GivenName, user.FamilyName, user.CreatedAt), cancellationToken);

    return user;
  }
}
