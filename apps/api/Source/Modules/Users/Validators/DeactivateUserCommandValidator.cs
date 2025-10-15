using FluentValidation;
using GameGuild.Database;


namespace GameGuild.Modules.Users.Validators;

/// <summary>
/// FluentValidation validator for DeactivateUserCommand
/// </summary>
public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand> {
  private readonly ApplicationDbContext _context;

  public DeactivateUserCommandValidator(ApplicationDbContext context) {
    _context = context;

    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required")
      .MustAsync(UserExists)
      .WithMessage("User not found")
      .MustAsync(UserIsActive)
      .WithMessage("User is already deactivated");
  }

  private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken) {
    return await _context.Users
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }

  private async Task<bool> UserIsActive(Guid userId, CancellationToken cancellationToken) {
    var user = await _context.Users
                             .FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);

    return user?.IsActive == true;
  }
}
