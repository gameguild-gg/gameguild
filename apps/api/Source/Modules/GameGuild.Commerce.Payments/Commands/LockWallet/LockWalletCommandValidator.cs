using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for LockWalletCommand
/// </summary>
public sealed class LockWalletCommandValidator : AbstractValidator<LockWalletCommand>
{
    public LockWalletCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Lock reason is required").MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
