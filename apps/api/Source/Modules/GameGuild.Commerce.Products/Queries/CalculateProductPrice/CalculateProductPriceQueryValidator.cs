using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for CalculateProductPriceQuery
/// </summary>
public sealed class CalculateProductPriceQueryValidator : AbstractValidator<CalculateProductPriceQuery>
{
    public CalculateProductPriceQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
