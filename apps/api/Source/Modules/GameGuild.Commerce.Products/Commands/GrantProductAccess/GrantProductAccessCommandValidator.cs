using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for GrantProductAccessCommand
/// </summary>
public class GrantProductAccessCommandValidator : AbstractValidator<GrantProductAccessCommand>
{
    public GrantProductAccessCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.PricePaid)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price paid cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters.");

        RuleFor(x => x.AccessEndDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Access end date must be in the future.")
            .When(x => x.AccessEndDate.HasValue);
    }
}
