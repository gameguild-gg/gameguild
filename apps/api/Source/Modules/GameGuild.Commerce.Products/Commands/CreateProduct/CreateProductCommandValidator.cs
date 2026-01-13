using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .WithMessage("Short description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .WithMessage("Image URL cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.ReferralCommissionPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Referral commission percentage must be between 0 and 100.");

        RuleFor(x => x.MaxAffiliateDiscount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max affiliate discount must be non-negative.");

        RuleFor(x => x.AffiliateCommissionPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Affiliate commission percentage must be between 0 and 100.");

        RuleFor(x => x.BundleItems)
            .NotEmpty()
            .WithMessage("Bundle items are required for bundles.")
            .When(x => x.IsBundle);
    }
}
