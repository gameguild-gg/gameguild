using FluentValidation;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Validator for AddOrderItemCommand
/// </summary>
public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("Quantity cannot exceed 100 items.");

        RuleFor(x => x.PromoCode)
            .MaximumLength(50)
            .WithMessage("Promo code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_-]*$")
            .WithMessage("Promo code can only contain letters, numbers, hyphens, and underscores.")
            .When(x => !string.IsNullOrEmpty(x.PromoCode));
    }
}
