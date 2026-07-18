using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for AddProductToOrderCommand
/// </summary>
public sealed class AddProductToOrderCommandValidator : AbstractValidator<AddProductToOrderCommand>
{
    public AddProductToOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.ProductPricingId).NotEmpty().WithMessage("Product pricing ID is required.");
        RuleFor(x => x.ProductPricingVersionId).NotEmpty().WithMessage("Product pricing version ID is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}
