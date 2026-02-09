using FluentValidation;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Validator for CreateSubscriptionPlanCommand
/// </summary>
public sealed class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
{
    public CreateSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required").MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Slug is required")
            .MaximumLength(50)
            .WithMessage("Slug cannot exceed 50 characters")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.MonthlyPriceInCents).GreaterThanOrEqualTo(0).WithMessage("MonthlyPriceInCents must be greater than or equal to 0");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters (ISO 4217)")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be uppercase letters only");

        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("Description cannot exceed 500 characters");
    }
}
