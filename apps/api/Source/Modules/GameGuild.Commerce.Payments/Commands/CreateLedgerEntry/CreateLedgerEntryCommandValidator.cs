using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for CreateLedgerEntryCommand
/// </summary>
public sealed class CreateLedgerEntryCommandValidator : AbstractValidator<CreateLedgerEntryCommand>
{
    public CreateLedgerEntryCommandValidator()
    {
        RuleFor(x => x.EntryType).NotEmpty().WithMessage("Entry type is required");

        RuleFor(x => x.DebitAccount).NotEmpty().WithMessage("Debit account is required").MaximumLength(50).WithMessage("Debit account cannot exceed 50 characters");

        RuleFor(x => x.CreditAccount).NotEmpty().WithMessage("Credit account is required").MaximumLength(50).WithMessage("Credit account cannot exceed 50 characters");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required").MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be a valid 3-character code");
    }
}
