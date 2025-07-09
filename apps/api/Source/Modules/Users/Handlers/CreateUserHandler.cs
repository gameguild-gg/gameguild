using GameGuild.Data;
using GameGuild.Modules.Users.Commands;
using GameGuild.Modules.Users.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Handlers;

/// <summary>
/// Handler for creating a new user with validation and business logic
/// </summary>
public class CreateUserHandler(
    ApplicationDbContext context, 
    ILogger<CreateUserHandler> logger,
    IMediator mediator) : IRequestHandler<CreateUserCommand, Models.User>
{
    public async Task<Models.User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists");
        }

        var user = new Models.User 
        { 
            Name = request.Name, 
            Email = request.Email, 
            IsActive = request.IsActive,
            Balance = request.InitialBalance,
            AvailableBalance = request.InitialBalance
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created with email {Email}", user.Id, user.Email);

        // Publish notification
        await mediator.Publish(new UserCreatedNotification
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt
        }, cancellationToken);

        return user;
    }
}
