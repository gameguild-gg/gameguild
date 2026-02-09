using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetProductByIdQuery
/// </summary>
public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
