using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetProductPricingQuery
/// </summary>
public sealed class GetProductPricingQueryValidator : AbstractValidator<GetProductPricingQuery>
{
    public GetProductPricingQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
