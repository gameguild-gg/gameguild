using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for ValidatePromoCodeCommand
/// </summary>
public sealed class ValidatePromoCodeCommandValidator : AbstractValidator<ValidatePromoCodeCommand>
{
    public ValidatePromoCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Promo code is required.");

        RuleFor(x => x.OrderAmount)
            .GreaterThan(0)
            .WithMessage("Order amount must be greater than 0.");
    }
}
