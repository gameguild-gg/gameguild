using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for SetProductPricingCommand
/// </summary>
public sealed class SetProductPricingCommandValidator : AbstractValidator<SetProductPricingCommand>
{
    public SetProductPricingCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pricing name is required.")
            .MaximumLength(100)
            .WithMessage("Pricing name cannot exceed 100 characters.");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Base price cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sale price cannot be negative.")
            .LessThan(x => x.BasePrice)
            .WithMessage("Sale price must be less than base price.")
            .When(x => x.SalePrice.HasValue);

        RuleFor(x => x.SaleEndDate)
            .GreaterThan(x => x.SaleStartDate)
            .WithMessage("Sale end date must be after sale start date.")
            .When(x => x.SaleStartDate.HasValue && x.SaleEndDate.HasValue);
    }
}
