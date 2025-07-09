using GameGuild.Data;
using GameGuild.Modules.Authentication.Commands;
using GameGuild.Modules.Users.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Authentication.Handlers;

/// <summary>
/// Handler for updating user profile with business logic and validation
/// </summary>
public class UpdateUserProfileHandler(ApplicationDbContext context) : IRequestHandler<UpdateUserProfileCommand, User> {
  public async Task<User> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken) {
    // Find the user
    var user =
      await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

    if (user == null) throw new InvalidOperationException($"User with ID {request.UserId} not found");

    // Business logic: Check if email is already taken by another user
    if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email) {
      var emailExists =
        await context.Users.AnyAsync(
          u => u.Email == request.Email && u.Id != request.UserId && !u.IsDeleted,
          cancellationToken
        );

      if (emailExists) throw new InvalidOperationException($"Email '{request.Email}' is already taken by another user");
    }

    // Update user properties
    if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;

    if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;

    if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

    // Update the entity (triggers BaseEntity.Touch())
    user.Touch();

    await context.SaveChangesAsync(cancellationToken);

    return user;
  }
}
