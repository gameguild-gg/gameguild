using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for CheckProductAccessQuery
/// </summary>
public class CheckProductAccessQueryValidator : AbstractValidator<CheckProductAccessQuery>
{
    public CheckProductAccessQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
