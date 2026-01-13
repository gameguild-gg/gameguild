using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for ApplyPromoCodesCommand
/// </summary>
public class ApplyPromoCodesCommandValidator : AbstractValidator<ApplyPromoCodesCommand>
{
    public ApplyPromoCodesCommandValidator()
    {
        RuleFor(x => x.OrderAmount)
            .GreaterThan(0)
            .WithMessage("Order amount must be greater than 0.");

        RuleFor(x => x.PromoCodes)
            .NotEmpty()
            .WithMessage("At least one promo code is required.");

        RuleForEach(x => x.PromoCodes)
            .NotEmpty()
            .WithMessage("Promo code cannot be empty.");
    }
}
