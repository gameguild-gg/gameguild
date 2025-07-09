using GameGuild.Data;
using GameGuild.Modules.Users.Commands;
using GameGuild.Modules.Users.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Users.Handlers;

/// <summary>
/// Handler for updating user information
/// </summary>
public class UpdateUserHandler(
    ApplicationDbContext context, 
    ILogger<UpdateUserHandler> logger,
    IMediator mediator) : IRequestHandler<UpdateUserCommand, Models.User>
{
    public async Task<Models.User> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Optimistic concurrency control
        if (request.ExpectedVersion.HasValue && user.Version != request.ExpectedVersion.Value)
        {
            throw new InvalidOperationException($"Concurrency conflict. Expected version {request.ExpectedVersion}, but current version is {user.Version}");
        }

        // Check for email uniqueness if email is being updated
        if (request.Email != null && request.Email != user.Email)
        {
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);
            
            if (existingUser != null)
            {
                throw new InvalidOperationException($"Email {request.Email} is already in use");
            }
        }

        // Track changes for notification
        var changes = new Dictionary<string, object>();

        // Update user properties
        if (request.Name != null && user.Name != request.Name)
        {
            changes["Name"] = new { From = user.Name, To = request.Name };
            user.Name = request.Name;
        }

        if (request.Email != null && user.Email != request.Email)
        {
            changes["Email"] = new { From = user.Email, To = request.Email };
            user.Email = request.Email;
        }

        if (request.IsActive.HasValue && user.IsActive != request.IsActive.Value)
        {
            changes["IsActive"] = new { From = user.IsActive, To = request.IsActive.Value };
            user.IsActive = request.IsActive.Value;
        }

        // Only save if there are actual changes
        if (changes.Any())
        {
            user.Touch();
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} updated successfully with {ChangeCount} changes", 
                request.UserId, changes.Count);

            // Publish notification
            await mediator.Publish(new UserUpdatedNotification
            {
                UserId = user.Id,
                UpdatedAt = user.UpdatedAt,
                Changes = changes
            }, cancellationToken);
        }

        return user;
    }
}

/// <summary>
/// Handler for deleting user
/// </summary>
public class DeleteUserHandler(
    ApplicationDbContext context, 
    ILogger<DeleteUserHandler> logger,
    IMediator mediator) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return false;
        }

        if (request.SoftDelete)
        {
            if (user.DeletedAt != null)
            {
                return false; // Already soft deleted
            }

            user.SoftDelete();
            logger.LogInformation("User {UserId} soft deleted", request.UserId);
        }
        else
        {
            context.Users.Remove(user);
            logger.LogInformation("User {UserId} permanently deleted", request.UserId);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Publish notification
        await mediator.Publish(new UserDeletedNotification
        {
            UserId = user.Id,
            DeletedAt = DateTime.UtcNow,
            SoftDelete = request.SoftDelete
        }, cancellationToken);

        return true;
    }
}

/// <summary>
/// Handler for restoring user
/// </summary>
public class RestoreUserHandler(
    ApplicationDbContext context, 
    ILogger<RestoreUserHandler> logger,
    IMediator mediator) : IRequestHandler<RestoreUserCommand, bool>
{
    public async Task<bool> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || user.DeletedAt == null)
        {
            return false;
        }

        user.Restore();
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} restored", request.UserId);

        // Publish notification
        await mediator.Publish(new UserRestoredNotification
        {
            UserId = user.Id,
            RestoredAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}

/// <summary>
/// Handler for updating user balance
/// </summary>
public class UpdateUserBalanceHandler(
    ApplicationDbContext context, 
    ILogger<UpdateUserBalanceHandler> logger,
    IMediator mediator) : IRequestHandler<UpdateUserBalanceCommand, Models.User>
{
    public async Task<Models.User> Handle(UpdateUserBalanceCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.DeletedAt == null, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Validate balance logic
        if (request.AvailableBalance > request.Balance)
        {
            throw new InvalidOperationException("Available balance cannot exceed total balance");
        }

        var oldBalance = user.Balance;
        var oldAvailableBalance = user.AvailableBalance;

        user.Balance = request.Balance;
        user.AvailableBalance = request.AvailableBalance;
        user.Touch();

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} balance updated from {OldBalance}/{OldAvailable} to {NewBalance}/{NewAvailable}. Reason: {Reason}", 
            request.UserId, oldBalance, oldAvailableBalance, request.Balance, request.AvailableBalance, request.Reason ?? "Not specified");

        // Publish notification
        await mediator.Publish(new UserBalanceUpdatedNotification
        {
            UserId = user.Id,
            OldBalance = oldBalance,
            NewBalance = request.Balance,
            OldAvailableBalance = oldAvailableBalance,
            NewAvailableBalance = request.AvailableBalance,
            Reason = request.Reason,
            UpdatedAt = user.UpdatedAt
        }, cancellationToken);

        return user;
    }
}
