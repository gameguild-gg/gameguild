using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for CalculateTaxCommand
/// </summary>
public sealed class CalculateTaxCommandValidator : AbstractValidator<CalculateTaxCommand>
{
    public CalculateTaxCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");

        RuleFor(x => x.JurisdictionCode).NotEmpty().WithMessage("Jurisdiction code is required").Length(2, 10).WithMessage("Jurisdiction code must be between 2 and 10 characters");

        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be a valid 3-character code");

        RuleFor(x => x.CustomerType).NotEmpty().WithMessage("Customer type is required");
    }
}
