using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for UpdatePromoCodeCommand
/// </summary>
public sealed class UpdatePromoCodeCommandValidator : AbstractValidator<UpdatePromoCodeCommand>
{
    public UpdatePromoCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Promo code ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(255)
            .WithMessage("Name cannot exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0.01m, 100m)
            .WithMessage("Discount percentage must be between 0.01 and 100.")
            .When(x => x.DiscountPercentage.HasValue);

        RuleFor(x => x.DiscountAmount)
            .GreaterThan(0)
            .WithMessage("Discount amount must be greater than 0.")
            .When(x => x.DiscountAmount.HasValue);

        RuleFor(x => x.Currency)
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.MinimumOrderAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum order amount cannot be negative.")
            .When(x => x.MinimumOrderAmount.HasValue);

        RuleFor(x => x.MaxUses)
            .GreaterThan(0)
            .WithMessage("Maximum uses must be greater than 0.")
            .When(x => x.MaxUses.HasValue);

        RuleFor(x => x.MaxUsesPerUser)
            .GreaterThan(0)
            .WithMessage("Maximum uses per user must be greater than 0.")
            .When(x => x.MaxUsesPerUser.HasValue);

        RuleFor(x => x.ValidUntil)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("Valid until date must be after valid from date.")
            .When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue);

        RuleFor(x => x.StackingPriority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stacking priority cannot be negative.")
            .When(x => x.StackingPriority.HasValue);
    }
}
