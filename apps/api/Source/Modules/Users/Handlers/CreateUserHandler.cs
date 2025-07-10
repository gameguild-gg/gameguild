using GameGuild.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Handler for creating a new user with validation and business logic
/// </summary>
public class CreateUserHandler(
    ApplicationDbContext context, 
    ILogger<CreateUserHandler> logger, 
    IMediator mediator) : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await context.Users
            .FirstOrDefaultAsync(user => user.Email == request.Email, cancellationToken);

        if (existingUser != null) 
            throw new InvalidOperationException($"User with email {request.Email} already exists");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            IsActive = request.IsActive,
            Balance = request.InitialBalance,
            AvailableBalance = request.InitialBalance,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created with email {Email}", user.Id, user.Email);

        // Publish domain event
        await mediator.Publish(new UserCreatedEvent(user.Id, user.Email, user.Name, user.CreatedAt), cancellationToken);

        return user;
    }
}
