using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users.Validators;

/// <summary>
/// FluentValidation validator for ActivateUserCommand
/// </summary>
public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
  private readonly ApplicationDbContext _context;

  public ActivateUserCommandValidator(ApplicationDbContext context)
  {
    _context = context;

    RuleFor(x => x.UserId)
      .NotEmpty().WithMessage("User ID is required")
      .MustAsync(UserExists).WithMessage("User not found")
      .MustAsync(UserIsDeactivated).WithMessage("User is already active");
  }

  private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
  {
    return await _context.Users
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }

  private async Task<bool> UserIsDeactivated(Guid userId, CancellationToken cancellationToken)
  {
    var user = await _context.Users
                             .FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);

    return user?.IsActive == false;
  }
}
