using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetPromoCodesQuery
/// </summary>
public class GetPromoCodesQueryValidator : AbstractValidator<GetPromoCodesQuery>
{
    public GetPromoCodesQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be non-negative.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, 100)
            .WithMessage("Take must be between 1 and 100.");
    }
}
