using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for UpdateProductCommand
/// </summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Name)
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .WithMessage("Description cannot exceed 4000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .WithMessage("Short description cannot exceed 500 characters.")
            .When(x => x.ShortDescription != null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .WithMessage("Image URL cannot exceed 500 characters.")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.ReferralCommissionPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Referral commission percentage must be between 0 and 100.")
            .When(x => x.ReferralCommissionPercentage.HasValue);

        RuleFor(x => x.MaxAffiliateDiscount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max affiliate discount must be non-negative.")
            .When(x => x.MaxAffiliateDiscount.HasValue);

        RuleFor(x => x.AffiliateCommissionPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Affiliate commission percentage must be between 0 and 100.")
            .When(x => x.AffiliateCommissionPercentage.HasValue);
    }
}
