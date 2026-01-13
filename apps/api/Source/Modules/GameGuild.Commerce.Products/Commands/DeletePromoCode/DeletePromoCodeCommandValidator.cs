using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for DeletePromoCodeCommand
/// </summary>
public class DeletePromoCodeCommandValidator : AbstractValidator<DeletePromoCodeCommand>
{
    public DeletePromoCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Promo code ID is required.");
    }
}
