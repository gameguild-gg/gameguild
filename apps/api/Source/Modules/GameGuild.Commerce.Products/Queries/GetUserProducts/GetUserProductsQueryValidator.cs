using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GetUserProductsQuery
/// </summary>
public class GetUserProductsQueryValidator : AbstractValidator<GetUserProductsQuery>
{
    public GetUserProductsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
