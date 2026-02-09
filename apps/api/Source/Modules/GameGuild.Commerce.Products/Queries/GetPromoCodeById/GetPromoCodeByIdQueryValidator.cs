using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetPromoCodeByIdQuery
/// </summary>
public sealed class GetPromoCodeByIdQueryValidator : AbstractValidator<GetPromoCodeByIdQuery>
{
    public GetPromoCodeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Promo code ID is required.");
    }
}
