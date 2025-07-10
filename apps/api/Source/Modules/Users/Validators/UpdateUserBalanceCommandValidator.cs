using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users.Validators;

/// <summary>
/// FluentValidation validator for UpdateUserBalanceCommand
/// </summary>
public class UpdateUserBalanceCommandValidator : AbstractValidator<UpdateUserBalanceCommand>
{
  private readonly ApplicationDbContext _context;

  public UpdateUserBalanceCommandValidator(ApplicationDbContext context)
  {
    _context = context;

    RuleFor(x => x.UserId)
      .NotEmpty().WithMessage("User ID is required")
      .MustAsync(UserExists).WithMessage("User not found");

    RuleFor(x => x.Balance)
      .GreaterThanOrEqualTo(0).WithMessage("Balance must be greater than or equal to 0");

    RuleFor(x => x.AvailableBalance)
      .GreaterThanOrEqualTo(0).WithMessage("Available balance must be greater than or equal to 0");

    RuleFor(x => x.Reason)
      .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

    RuleFor(x => x.ExpectedVersion)
      .GreaterThan(0)
      .When(x => x.ExpectedVersion.HasValue)
      .WithMessage("Expected version must be greater than 0");

    // Business rule: Available balance should not exceed total balance
    RuleFor(x => x.AvailableBalance)
      .LessThanOrEqualTo(x => x.Balance)
      .WithMessage("Available balance cannot exceed total balance");
  }

  private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
  {
    return await _context.Resources.OfType<User>()
                         .AnyAsync(x => x.Id == userId && x.DeletedAt == null, cancellationToken);
  }
}
