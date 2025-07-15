using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users.Validators;

/// <summary>
/// FluentValidation validator for DeleteUserCommand
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand> {
  private readonly ApplicationDbContext _context;

  public DeleteUserCommandValidator(ApplicationDbContext context) {
    _context = context;

    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required")
      .MustAsync(UserExists)
      .WithMessage("User not found");
  }

  private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken) {
    return await _context.Users
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }
}
