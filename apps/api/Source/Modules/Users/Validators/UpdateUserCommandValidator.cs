using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users.Validators;

/// <summary>
/// FluentValidation validator for UpdateUserCommand
/// </summary>
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
  private readonly ApplicationDbContext _context;

  public UpdateUserCommandValidator(ApplicationDbContext context)
  {
    _context = context;

    RuleFor(x => x.UserId)
      .NotEmpty().WithMessage("User ID is required")
      .MustAsync(UserExists).WithMessage("User not found");

    RuleFor(x => x.Name)
      .Length(1, 100).WithMessage("Name must be between 1 and 100 characters")
      .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods")
      .When(x => !string.IsNullOrEmpty(x.Name));

    RuleFor(x => x.Email)
      .EmailAddress().WithMessage("Invalid email format")
      .Length(1, 255).WithMessage("Email must be between 1 and 255 characters")
      .MustAsync(BeUniqueEmailForUpdate).WithMessage("Email address is already in use")
      .When(x => !string.IsNullOrEmpty(x.Email));
  }

  private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
  {
    return await _context.Users
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }

  private async Task<bool> BeUniqueEmailForUpdate(UpdateUserCommand command, string email, CancellationToken cancellationToken)
  {
    return !await _context.Users
                          .AnyAsync(x => x.Email == email && x.Id != command.UserId && x.DeletedAt == null, cancellationToken);
  }
}
