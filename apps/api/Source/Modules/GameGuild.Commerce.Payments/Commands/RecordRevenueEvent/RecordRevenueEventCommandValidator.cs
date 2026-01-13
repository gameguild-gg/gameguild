using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for RecordRevenueEventCommand
/// </summary>
public sealed class RecordRevenueEventCommandValidator : AbstractValidator<RecordRevenueEventCommand>
{
    public RecordRevenueEventCommandValidator()
    {
        RuleFor(x => x.EventType).NotEmpty().WithMessage("Event type is required");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Revenue amount must be greater than zero");

        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be a valid 3-character code");

        RuleFor(x => x.Source).NotEmpty().WithMessage("Revenue source is required");

        RuleFor(x => x.ReferenceId).NotEmpty().WithMessage("Reference ID is required").MaximumLength(100).WithMessage("Reference ID cannot exceed 100 characters");

        RuleFor(x => x.Metadata).MaximumLength(2000).WithMessage("Metadata cannot exceed 2000 characters").When(x => !string.IsNullOrEmpty(x.Metadata));
    }
}
